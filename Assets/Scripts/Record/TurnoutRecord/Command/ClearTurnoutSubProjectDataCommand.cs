using QFramework;

public class ClearTurnoutSubProjectDataCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutRecordModel>().SubProjects.Clear();
        this.SendEvent<ModifyTurnoutSubProjectDataEvent>();
    }
}