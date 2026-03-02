using System;
using SQLite4Unity3d;

[Table("TurnoutSubProjectData")]
public class TurnoutSubProjectData
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Indexed, NotNull] public int ProjectId { get; set; }

    [NotNull] public string SubProjectName { get; set; }

    [NotNull] public string UserId { get; set; }

    [NotNull] // 直接存字符串，无转换
    public string Time { get; set; } = "1970-01-01T00:00:00";

    [Indexed, NotNull] public int StationId { get; set; }


    public TurnoutSubProjectData()
    {
    }

    public TurnoutSubProjectData(int ProjectId, string subProjectName, string userId, int StationId)
    {
        this.ProjectId = ProjectId;
        this.SubProjectName = subProjectName;
        this.UserId = userId;
        this.StationId = StationId;
    }
}