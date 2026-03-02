
using SQLite4Unity3d;

// ========== 项目表 ==========
[Table("ProjectData")]
public class ProjectData
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Unique, NotNull] public string ProjectName { get; set; }
}