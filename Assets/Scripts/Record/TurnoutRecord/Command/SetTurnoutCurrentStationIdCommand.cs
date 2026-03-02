using QFramework;

public class SetTurnoutCurrentStationIdCommand : AbstractCommand
{
    private int currentStationId;

    public SetTurnoutCurrentStationIdCommand(int currentProjectId)
    {
        this.currentStationId = currentProjectId;
    }

    protected override void OnExecute()
    {
        this.GetModel<TurnoutRecordModel>().currentStationId = this.currentStationId;
    }
}