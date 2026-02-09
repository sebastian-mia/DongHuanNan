using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class PerpareUpSideToTranslate2RouteCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().targetRoute
            .SetValueWithoutEvent(new Route(TurnoutID.Turnout02, RedLightPosition.上行));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout02,
            new TurnoutMachine(TurnoutID.Turnout02, LockedState.反位钩锁, IsPowerCut.已断电, TurnoutState.反位));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout05,
            new TurnoutMachine(TurnoutID.Turnout05, LockedState.定位钩锁, IsPowerCut.已断电, TurnoutState.定位));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout04,
            new TurnoutMachine(TurnoutID.Turnout04, LockedState.定位钩锁, IsPowerCut.已断电, TurnoutState.定位));
    }
}