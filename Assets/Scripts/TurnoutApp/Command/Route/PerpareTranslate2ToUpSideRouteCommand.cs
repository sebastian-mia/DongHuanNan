using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class PerpareTranslate2ToUpSideRouteCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().targetRoute.SetValueWithoutEvent(new Route(TurnoutID.Turnout04, RedLightPosition.折返线2道));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout02,
            new TurnoutMachine(TurnoutID.Turnout02, LockedState.Nothing, false, TurnoutState.Reverse));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout05,
            new TurnoutMachine(TurnoutID.Turnout05, LockedState.Nothing, false, TurnoutState.Normal));
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Add(TurnoutID.Turnout04,
            new TurnoutMachine(TurnoutID.Turnout04, LockedState.Nothing, false, TurnoutState.Normal));
    }
}