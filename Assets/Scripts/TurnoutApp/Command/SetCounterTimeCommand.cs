using QFramework;

public class SetCounterTimeCommand : AbstractCommand
{
    private string CounterTime;

    public SetCounterTimeCommand(string counterTime)
    {
        this.CounterTime = counterTime;
    }

    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().counterTime = CounterTime;
    }
}