// ========== 线路-车站关联表 ==========

using SQLite4Unity3d;

[Table("LineStationMap")]
public class LineStationMap
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Indexed, NotNull] public int LineID { get; set; }

    [Indexed, NotNull] public int StationID { get; set; }
}