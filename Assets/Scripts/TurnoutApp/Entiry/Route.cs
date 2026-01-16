using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Route
{
    private TurnoutID startTurnoutID;
    public Dictionary<TurnoutID, TurnoutMachine> turnoutMachines = new Dictionary<TurnoutID, TurnoutMachine>();
    public bool hasRedLight;

    public Route(TurnoutID startTurnoutID, bool hasRedLight)
    {
        this.startTurnoutID = startTurnoutID;
        this.hasRedLight = hasRedLight;
    }

    public TurnoutID StartTurnoutID
    {
        get => startTurnoutID;
        set => startTurnoutID = value;
    }

    public Dictionary<TurnoutID, TurnoutMachine> TurnoutMachines
    {
        get => turnoutMachines;
        set => turnoutMachines = value;
    }

    public bool HasRedLight
    {
        get => hasRedLight;
        set => hasRedLight = value;
    }
}