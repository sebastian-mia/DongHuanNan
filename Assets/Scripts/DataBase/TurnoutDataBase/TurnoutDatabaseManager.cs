using UnityEngine;
using SQLite4Unity3d; // 变更：使用 SQLite4Unity3d 命名空间
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QFramework;

public class TurnoutDatabaseManager : IUtility
{
    public static TurnoutDatabaseManager Instance { get; private set; }

    private SQLiteConnection _connection;
    private string _dbPath;

    private const int MAX_RECORDS_PER_STATION_PROJECT = 100;

    public bool IsConnected => _connection != null;

    public TurnoutDatabaseManager()
    {
        InitializeDatabase();

        var fileInfo = new FileInfo(_dbPath);
        Debug.Log($"[DB] 数据库大小: {fileInfo.Length} bytes, 路径: {_dbPath}");
        Debug.Log(
            $"[DB] Projects: {GetAllProjects().Count}, Lines: {GetAllLines().Count}, Stations: {GetAllStations().Count}");
    }

    private void InitializeDatabase()
    {
        try
        {
            _dbPath = Path.Combine(Application.persistentDataPath, "TurnoutDatabase.db");
            Debug.Log($"[TurnoutDataManager] 数据库路径: {_dbPath}");

            // 检查是否需要从 StreamingAssets 复制数据库
            if (!File.Exists(_dbPath) || new FileInfo(_dbPath).Length == 0)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Android 平台：使用协程复制（SQLite4Unity3d 推荐方式）
                // 注意：构造函数中无法使用协程，需要延迟初始化或改用同步方式
                CopyDatabaseFromStreamingAssetsAndroidSync();
#else
                // PC/Editor 直接复制
                CopyDatabaseFromStreamingAssets();
#endif
            }

            // 移除 SQLitePCLRaw 初始化（SQLite4Unity3d 不需要）
            // InitializeSQLitePCL();

            // 变更：使用 SQLite4Unity3d 的连接方式，添加 Open 标志
            _connection = new SQLiteConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
            CreateTablesAndIndexes();

            Debug.Log("[TurnoutDataManager] 数据库初始化成功");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TurnoutDataManager] 初始化失败: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 已移除：SQLite4Unity3d 不需要 SQLitePCLRaw 初始化
    /// </summary>
    private void InitializeSQLitePCL()
    {
        // SQLite4Unity3d 内部已处理原生库加载，此方法保留为空
        Debug.Log("[TurnoutDataManager] SQLite4Unity3d 不需要额外初始化");
    }

    /// <summary>
    /// PC/Editor 平台：直接复制预置数据库
    /// </summary>
    private void CopyDatabaseFromStreamingAssets()
    {
        string sourcePath = Path.Combine(Application.streamingAssetsPath, "TurnoutDatabase.db");
        Debug.Log($"[TurnoutDataManager] 尝试从 StreamingAssets 复制: {sourcePath}");

        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"[TurnoutDataManager] StreamingAssets 中无数据库，将创建空数据库");
            return;
        }

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        File.Copy(sourcePath, _dbPath);
        Debug.Log($"[TurnoutDataManager] 数据库复制成功，大小: {new FileInfo(_dbPath).Length} bytes");
    }

    /// <summary>
    /// Android 平台：使用同步 WWW 读取（避免线程问题）
    /// </summary>
    private void CopyDatabaseFromStreamingAssetsAndroidSync()
    {
        string sourcePath = Path.Combine(Application.streamingAssetsPath, "TurnoutDatabase.db");
        Debug.Log($"[TurnoutDataManager] Android 尝试从 StreamingAssets 解压: {sourcePath}");

        try
        {
            byte[] dbBytes = ReadFileFromStreamingAssetsSync(sourcePath);

            if (dbBytes == null || dbBytes.Length == 0)
            {
                Debug.LogWarning($"[TurnoutDataManager] StreamingAssets 中无数据库，将创建空数据库");
                return;
            }

            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }

            File.WriteAllBytes(_dbPath, dbBytes);
            Debug.Log($"[TurnoutDataManager] Android 数据库解压成功，大小: {dbBytes.Length} bytes");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TurnoutDataManager] Android 解压失败: {e.Message}");
        }
    }

    /// <summary>
    /// 同步读取 StreamingAssets（Android 兼容）
    /// </summary>
    private byte[] ReadFileFromStreamingAssetsSync(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android: 使用 WWW 同步读取（比 UnityWebRequest 更稳定）
        using (WWW www = new WWW(path))
        {
            // 同步等待（构造函数中无法使用协程）
            while (!www.isDone) { }
            
            if (string.IsNullOrEmpty(www.error))
            {
                return www.bytes;
            }
            else
            {
                Debug.LogError($"[TurnoutDataManager] WWW 读取失败: {www.error}");
                return null;
            }
        }
#else
        // PC/Editor: 直接读取
        if (File.Exists(path))
        {
            return File.ReadAllBytes(path);
        }

        return null;
#endif
    }

    private void CreateTablesAndIndexes()
    {
        _connection.CreateTable<ProjectData>();
        _connection.CreateTable<LineData>();
        _connection.CreateTable<StationData>();
        _connection.CreateTable<LineStationMap>();
        _connection.CreateTable<TurnoutSubProjectData>();
        _connection.CreateTable<TurnoutNodeData>();

        // 索引使用 Time 列（现在是 string）
        // 变更：SQLite4Unity3d 使用 Execute 方法执行原始 SQL
        _connection.Execute(@"
            CREATE INDEX IF NOT EXISTS idx_subproject_station_project 
            ON TurnoutSubProjectData(StationId, ProjectId, Time)
        ");

        _connection.Execute(@"
            CREATE UNIQUE INDEX IF NOT EXISTS idx_subproject_unique 
            ON TurnoutSubProjectData(ProjectId, SubProjectName, UserId,CounterTime, Time)
        ");

        _connection.Execute(@"
            CREATE INDEX IF NOT EXISTS idx_node_subproject 
            ON TurnoutNodeData(SubProjectId, UserId, Time)
        ");

        _connection.Execute(@"
            CREATE UNIQUE INDEX IF NOT EXISTS idx_node_unique 
            ON TurnoutNodeData(SubProjectId, UserId, NodeName, Time)
        ");
    }

    void OnDestroy()
    {
        _connection?.Close();
    }

    #region 基础数据管理

    public int InsertProject(string projectName)
    {
        return _connection.Insert(new ProjectData { ProjectName = projectName });
    }

    public int InsertLine(string lineName)
    {
        return _connection.Insert(new LineData { LineName = lineName });
    }

    public int InsertStation(string stationName)
    {
        return _connection.Insert(new StationData { StationName = stationName });
    }

    public void MapLineStation(int lineId, int stationId)
    {
        _connection.Insert(new LineStationMap { LineID = lineId, StationID = stationId });
    }

    public List<StationData> GetStationsByLine(int lineId)
    {
        var sql = @"
            SELECT s.* FROM StationData s
            INNER JOIN LineStationMap m ON s.Id = m.StationID
            WHERE m.LineID = ?
            ORDER BY s.StationName";
        // 变更：SQLite4Unity3d 的 Query 方法使用 params 参数
        return _connection.Query<StationData>(sql, lineId);
    }

    public string GetProjectName(int projectId)
    {
        // 变更：使用 Find 替代 Get（SQLite4Unity3d 推荐）
        var project = _connection.Find<ProjectData>(projectId);
        return project?.ProjectName;
    }

    public string GetStationName(int stationId)
    {
        var station = _connection.Find<StationData>(stationId);
        return station?.StationName;
    }

    public List<LineData> GetAllLines()
    {
        return _connection.Table<LineData>().ToList();
    }

    public List<StationData> GetAllStations()
    {
        return _connection.Table<StationData>().ToList();
    }

    public List<ProjectData> GetAllProjects()
    {
        return _connection.Table<ProjectData>().ToList();
    }

    public string GetLineName(int lineId)
    {
        var line = _connection.Find<LineData>(lineId);
        return line?.LineName;
    }

    #endregion

    #region 道岔项目插入（覆盖模式）

    public int InsertTurnoutProject(
        int projectId,
        string subProjectName,
        string userId,
        int stationId,
        string time,
        string counterTime,
        List<TurnoutNodeData> nodes)
    {
        // 变更：使用 FirstOrDefault 的 LINQ 语法
        var existing = _connection.Table<TurnoutSubProjectData>()
            .Where(s => s.ProjectId == projectId
                        && s.SubProjectName == subProjectName
                        && s.UserId == userId
                        && s.Time == time)
            .FirstOrDefault();

        if (existing != null)
        {
            _connection.Execute(@"
                DELETE FROM TurnoutNodeData 
                WHERE SubProjectId = ? AND UserId = ? AND Time = ?",
                existing.Id, userId, time);

            _connection.Delete(existing);
            Debug.Log($"[TurnoutDataManager] 覆盖旧记录: {time}");
        }

        var subProject = new TurnoutSubProjectData
        {
            ProjectId = projectId,
            SubProjectName = subProjectName,
            UserId = userId,
            Time = time,
            CounterTime = counterTime,
            StationId = stationId
        };
        _connection.Insert(subProject);

        foreach (var node in nodes)
        {
            node.SubProjectId = subProject.Id;
            node.UserId = userId;
            node.Time = time;
            _connection.Insert(node);
        }

        CleanupOldRecords(stationId, projectId);
        return subProject.Id;
    }

    #endregion

    #region 根据名称获取ID

    public int? GetProjectId(string projectName)
    {
        var project = _connection.Table<ProjectData>()
            .Where(p => p.ProjectName == projectName)
            .FirstOrDefault();
        return project?.Id;
    }

    public int? GetLineId(string lineName)
    {
        var line = _connection.Table<LineData>()
            .Where(l => l.LineName == lineName)
            .FirstOrDefault();
        return line?.Id;
    }

    public int? GetStationId(string stationName)
    {
        var station = _connection.Table<StationData>()
            .Where(s => s.StationName == stationName)
            .FirstOrDefault();
        return station?.Id;
    }

    public int? GetSubProjectId(string subProjectName, string userId, string time)
    {
        var subProject = _connection.Table<TurnoutSubProjectData>()
            .Where(s => s.SubProjectName == subProjectName
                        && s.UserId == userId
                        && s.Time == time)
            .FirstOrDefault();
        return subProject?.Id;
    }

    public int? GetLatestSubProjectId(string subProjectName, string userId)
    {
        var subProject = _connection.Table<TurnoutSubProjectData>()
            .Where(s => s.SubProjectName == subProjectName && s.UserId == userId)
            .OrderByDescending(s => s.Time)
            .FirstOrDefault();
        return subProject?.Id;
    }

    public int? GetNodeId(int subProjectId, string userId, string time, string nodeName)
    {
        var node = _connection.Table<TurnoutNodeData>()
            .Where(n => n.SubProjectId == subProjectId
                        && n.UserId == userId
                        && n.Time == time
                        && n.NodeName == nodeName)
            .FirstOrDefault();
        return node?.Id;
    }

    public int? GetLatestNodeId(int subProjectId, string nodeName)
    {
        var node = _connection.Table<TurnoutNodeData>()
            .Where(n => n.SubProjectId == subProjectId && n.NodeName == nodeName)
            .OrderByDescending(n => n.Time)
            .FirstOrDefault();
        return node?.Id;
    }

    #endregion

    #region 清理方法（级联删除）

    private void CleanupOldRecords(int stationId, int projectId)
    {
        var sql = @"
            SELECT Id FROM TurnoutSubProjectData 
            WHERE StationId = ? AND ProjectId = ?
            ORDER BY Time DESC 
            LIMIT -1 OFFSET ?";

        var oldRecords = _connection.Query<IdOnly>(sql, stationId, projectId, MAX_RECORDS_PER_STATION_PROJECT);

        if (oldRecords.Count == 0) return;

        var ids = oldRecords.Select(r => r.Id).ToList();
        var idList = string.Join(",", ids);

        _connection.Execute($@"
            DELETE FROM TurnoutNodeData 
            WHERE SubProjectId IN ({idList})");

        _connection.Execute($@"
            DELETE FROM TurnoutSubProjectData 
            WHERE Id IN ({idList})");

        Debug.Log($"[TurnoutDataManager] 清理 {oldRecords.Count} 条旧记录");
    }

    private class IdOnly
    {
        public int Id { get; set; }
    }

    #endregion

    #region 更新结点状态

    public bool UpdateNodeState(int subProjectId, string userId, string time, string nodeName,
        string targetState, string currentState, bool isConsistent)
    {
        var node = _connection.Table<TurnoutNodeData>()
            .Where(n => n.SubProjectId == subProjectId
                        && n.UserId == userId
                        && n.Time == time
                        && n.NodeName == nodeName)
            .FirstOrDefault();

        if (node == null) return false;

        node.TargetState = targetState;
        node.CurrentState = currentState;
        node.IsConsistent = isConsistent;

        _connection.Update(node);
        return true;
    }

    #endregion

    #region 查询方法

    public List<TurnoutSubProjectData> GetSubProjectList(int projectId, int stationId)
    {
        return _connection.Table<TurnoutSubProjectData>()
            .Where(s => s.ProjectId == projectId && s.StationId == stationId)
            .OrderByDescending(s => s.Time)
            .ToList();
    }

    public List<TurnoutSubProjectData> GetSubProjectList(int projectId, int stationId, string userId)
    {
        return _connection.Table<TurnoutSubProjectData>()
            .Where(s => s.ProjectId == projectId && s.StationId == stationId && s.UserId == userId)
            .OrderByDescending(s => s.Time)
            .ToList();
    }

    public List<TurnoutSubProjectData> GetSubProjectList(int projectId, int stationId,
        string subProjectName, string userId)
    {
        return _connection.Table<TurnoutSubProjectData>()
            .Where(s => s.ProjectId == projectId && s.StationId == stationId
                                                 && s.SubProjectName == subProjectName && s.UserId == userId)
            .OrderByDescending(s => s.Time)
            .ToList();
    }

    public List<TurnoutNodeData> GetNodesBySubProject(string subProjectName, string userId, string time)
    {
        var sql = @"
            SELECT n.* FROM TurnoutNodeData n
            INNER JOIN TurnoutSubProjectData s ON n.SubProjectId = s.Id
            WHERE s.SubProjectName = ? AND s.UserId = ? AND s.Time = ? 
              AND n.UserId = ? AND n.Time = ?";

        return _connection.Query<TurnoutNodeData>(sql, subProjectName, userId, time, userId, time);
    }

    public (TurnoutSubProjectData subProject, List<TurnoutNodeData> nodes) GetLatestRecord(
        int projectId, string subProjectName, string userId)
    {
        var subProject = _connection.Table<TurnoutSubProjectData>()
            .Where(s => s.ProjectId == projectId
                        && s.SubProjectName == subProjectName
                        && s.UserId == userId)
            .OrderByDescending(s => s.Time)
            .FirstOrDefault();

        if (subProject == null) return (null, new List<TurnoutNodeData>());

        var nodes = GetNodesBySubProject(subProject.SubProjectName, subProject.UserId, subProject.Time);
        return (subProject, nodes);
    }

    public List<TurnoutSubProjectData> GetRecordsByStationProject(int stationId, int projectId)
    {
        return _connection.Table<TurnoutSubProjectData>()
            .Where(s => s.StationId == stationId && s.ProjectId == projectId)
            .OrderByDescending(s => s.Time)
            .ToList();
    }

    #endregion

    #region 通用 CRUD

    public int Insert<T>(T data) where T : new() => _connection.Insert(data);
    public int Update<T>(T data) where T : new() => _connection.Update(data);
    public int Delete<T>(T data) where T : new() => _connection.Delete(data);

    // 变更：使用 Find 替代 Get（SQLite4Unity3d 推荐方式）
    public T GetById<T>(object primaryKey) where T : new() => _connection.Find<T>(primaryKey);
    public List<T> GetAll<T>() where T : new() => _connection.Table<T>().ToList();
    public TableQuery<T> Table<T>() where T : new() => _connection.Table<T>();

    #endregion
}