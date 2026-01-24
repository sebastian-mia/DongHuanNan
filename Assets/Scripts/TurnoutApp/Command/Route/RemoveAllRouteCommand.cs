using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class RemoveAllRouteCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines.Clear();
    }
}