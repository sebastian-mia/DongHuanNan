using QFramework;

public class ClearTurnoutCurrentStationIdCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutRecordModel>().currentStationId = 0;
    }
}