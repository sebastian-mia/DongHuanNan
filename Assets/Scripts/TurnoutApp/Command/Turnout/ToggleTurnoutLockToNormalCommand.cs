using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class ToggleTurnoutLockToNormalCommand : AbstractCommand
{
    private TurnoutID turnoutID;

    public ToggleTurnoutLockToNormalCommand(TurnoutID turnoutID)
    {
        this.turnoutID = turnoutID;
    }

    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines[turnoutID].LockedState = LockedState.定位钩锁;
    }
}