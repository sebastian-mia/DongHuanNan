using QFramework;

public class SetCurrentUserIdCommand : AbstractCommand
{
    private string userId;

    public SetCurrentUserIdCommand(string userId)
    {
        this.userId = userId;
    }

    protected override void OnExecute()
    {
        this.GetModel<GameRecordModel>().UserId = this.userId;
    }
}