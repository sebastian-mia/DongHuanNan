// ========== 车站表 ==========
using SQLite4Unity3d;

[Table("StationData")]
public class StationData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique, NotNull]
    public string StationName { get; set; }
}