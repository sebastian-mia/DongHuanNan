using QFramework;

public class ClearTurnoutNodeDataCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutRecordModel>().Nodes.Clear();
        this.SendEvent<ModifyTurnoutNodeDataEvent>();
    }
}