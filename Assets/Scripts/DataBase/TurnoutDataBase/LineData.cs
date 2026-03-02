// ========== 线路表 ==========

using SQLite4Unity3d;

[Table("LineData")]
public class LineData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Unique, NotNull]
    public string LineName { get; set; }
}