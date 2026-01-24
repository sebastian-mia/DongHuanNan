using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class ToggleTurnoutToNormalCommand : AbstractCommand
{
    private TurnoutID turnoutID;

    public ToggleTurnoutToNormalCommand(TurnoutID turnoutID)
    {
        this.turnoutID = turnoutID;
    }

    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[turnoutID].TurnoutState = TurnoutState.Normal;
    }
}