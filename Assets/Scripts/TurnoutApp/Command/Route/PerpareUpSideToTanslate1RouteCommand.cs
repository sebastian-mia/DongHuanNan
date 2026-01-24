using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class PerpareUpSideToTranslate1RouteCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().targetRoute.SetValueWithoutEvent(new Route(TurnoutID.Turnout02, true));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout02,
            new TurnoutMachine(TurnoutID.Turnout02, LockedState.Nothing, false, TurnoutState.Reverse));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout05,
            new TurnoutMachine(TurnoutID.Turnout05, LockedState.Nothing, false, TurnoutState.Reverse));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout06,
            new TurnoutMachine(TurnoutID.Turnout06, LockedState.Nothing, false, TurnoutState.Reverse));
    }
}