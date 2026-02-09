using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class ToggleTurnoutLockToNothingCommand : AbstractCommand
{
    private TurnoutID turnoutID;

    public ToggleTurnoutLockToNothingCommand(TurnoutID turnoutID)
    {
        this.turnoutID = turnoutID;
    }

    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[turnoutID].LockedState = LockedState.未加锁;
    }
}