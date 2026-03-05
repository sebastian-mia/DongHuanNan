using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class PerpareDownSideToDownSideRouteCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().targetRoute
            .SetValueWithoutEvent(new Route(TurnoutID.Turnout01, RedLightPosition.下行正线));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout01,
            new TurnoutMachine(TurnoutID.Turnout01, LockedState.定位钩锁, IsPowerCut.已断电, TurnoutState.定位));
    }
}