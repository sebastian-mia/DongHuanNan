using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class PerpareDownSideToTranslate1RouteCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().targetRoute.SetValueWithoutEvent(new Route(TurnoutID.Turnout01, true));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout01,
            new TurnoutMachine(TurnoutID.Turnout01, LockedState.Nothing, false, TurnoutState.Reverse));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout03,
            new TurnoutMachine(TurnoutID.Turnout03, LockedState.Nothing, false, TurnoutState.Normal));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout06,
            new TurnoutMachine(TurnoutID.Turnout06, LockedState.Nothing, false, TurnoutState.Normal));
    }
}