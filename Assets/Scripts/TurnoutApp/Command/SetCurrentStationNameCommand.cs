using QFramework;

public class SetCurrentStationNameCommand : AbstractCommand
{
    private int currentStationId;

    public SetCurrentStationNameCommand(int currentStationId)
    {
        this.currentStationId = currentStationId;
    }

    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().currentStationId = this.currentStationId;
    }
}