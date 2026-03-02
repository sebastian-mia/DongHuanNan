using QFramework;

public class ClearStationDataCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutRecordModel>().Stations.Clear();
        this.SendEvent<ModifyStationDataEvent>();
    }
}