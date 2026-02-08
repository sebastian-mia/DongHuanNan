using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class PerpareUpSideToUpSideRouteCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().routeCount = 1;
        this.GetModel<TurnoutModel>().targetRoute
            .SetValueWithoutEvent(new Route(TurnoutID.Turnout02, RedLightPosition.上行));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout02,
            new TurnoutMachine(TurnoutID.Turnout02, LockedState.Nothing, false, TurnoutState.Normal));
    }
}