using QFramework;

public class ClearTurnoutCurrentProjectIdCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutRecordModel>().currentprojectId = 0;
    }
}