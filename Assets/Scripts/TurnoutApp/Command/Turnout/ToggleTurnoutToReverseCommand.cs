using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class ToggleTurnoutToReverseCommand : AbstractCommand
{
    private TurnoutID turnoutID;

    public ToggleTurnoutToReverseCommand(TurnoutID turnoutID)
    {
        this.turnoutID = turnoutID;
    }

    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[turnoutID].TurnoutState = TurnoutState.Reverse;
    }
}