using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class TurnoutApp : Architecture<TurnoutApp>
{
    protected override void Init()
    {
        this.RegisterModel(new TurnoutModel());
    }
}