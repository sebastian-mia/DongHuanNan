using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class PerpareTranslate1ToUpSideRouteCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().targetRoute
            .SetValueWithoutEvent(new Route(TurnoutID.Turnout06, RedLightPosition.折返线1道));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout02,
            new TurnoutMachine(TurnoutID.Turnout02, LockedState.反位钩锁, IsPowerCut.已断电, TurnoutState.反位));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout05,
            new TurnoutMachine(TurnoutID.Turnout05, LockedState.反位钩锁, IsPowerCut.已断电, TurnoutState.反位));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout06,
            new TurnoutMachine(TurnoutID.Turnout06, LockedState.反位钩锁, IsPowerCut.已断电, TurnoutState.反位));
    }
}