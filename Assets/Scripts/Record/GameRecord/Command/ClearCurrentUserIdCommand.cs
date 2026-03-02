using QFramework;

public class ClearCurrentUserIdCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<GameRecordModel>().UserId = "";
    }
}