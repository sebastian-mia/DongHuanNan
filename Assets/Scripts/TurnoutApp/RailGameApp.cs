using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class RailGameApp : Architecture<RailGameApp>
{
    protected override void Init()
    {
        //Model
        this.RegisterModel(new TurnoutModel());
        this.RegisterModel(new TurnoutRecordModel());
        this.RegisterModel(new GameRecordModel());
        //Utility
        this.RegisterUtility(new RandomNumber());
        this.RegisterUtility(new TurnoutDatabaseManager());
        this.RegisterUtility(new EnumUtils());
        this.RegisterUtility(new Validator());
        this.RegisterUtility(new TimeFormatter());
        this.RegisterUtility(new AudioManager());
        //System
    }
}