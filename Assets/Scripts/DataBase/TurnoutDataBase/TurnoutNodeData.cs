using System;
using SQLite4Unity3d;

[Table("TurnoutNodeData")]
public class TurnoutNodeData
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Indexed, NotNull] public int SubProjectId { get; set; }

    [NotNull] public string UserId { get; set; }

    [NotNull] // 直接存字符串，无转换
    public string Time { get; set; } = "1970-01-01T00:00:00";

    [NotNull] public string NodeName { get; set; }

    public string TargetState { get; set; }

    public string CurrentState { get; set; }

    public bool IsConsistent { get; set; }


    public TurnoutNodeData()
    {
    }

    public TurnoutNodeData(string nodeName, string targetState, string currentState,
        bool isConsistent)
    {
        this.NodeName = nodeName;
        this.TargetState = targetState;
        this.CurrentState = currentState;
        this.IsConsistent = isConsistent;
    }
}