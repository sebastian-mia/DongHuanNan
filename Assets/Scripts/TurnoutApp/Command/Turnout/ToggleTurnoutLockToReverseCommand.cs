using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class ToggleTurnoutLockToReverseCommand : AbstractCommand
{
    private TurnoutID turnoutID;

    public ToggleTurnoutLockToReverseCommand(TurnoutID turnoutID)
    {
        this.turnoutID = turnoutID;
    }

    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[turnoutID].LockedState = LockedState.反位钩锁;
    }
}