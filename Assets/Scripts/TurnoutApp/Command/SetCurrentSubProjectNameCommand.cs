using QFramework;

public class SetCurrentSubProjectNameCommand : AbstractCommand
{
    private string currentSubProjectName;

    public SetCurrentSubProjectNameCommand(string currentProjectName)
    {
        this.currentSubProjectName = currentProjectName;
    }

    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().currentsubProjectName = this.currentSubProjectName;
    }
}