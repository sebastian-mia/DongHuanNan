using QFramework;

public class ClearLineDataCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutRecordModel>().Lines.Clear();
        this.SendEvent<ModifyLineDataEvent>();
    }
}