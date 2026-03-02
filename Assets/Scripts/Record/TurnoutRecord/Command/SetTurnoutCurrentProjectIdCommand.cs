using QFramework;

public class SetTurnoutCurrentProjectIdCommand : AbstractCommand
{
    private int currentProjectId;

    public SetTurnoutCurrentProjectIdCommand(int currentProjectId)
    {
        this.currentProjectId = currentProjectId;
    }

    protected override void OnExecute()
    {
        this.GetModel<TurnoutRecordModel>().currentprojectId = this.currentProjectId;
    }
}