using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TurnoutState
{
    定位,
    反位,
    无位置
}

public enum LockedState
{
    定位钩锁,
    反位钩锁,
    未加锁
}

public enum TurnoutID
{
    Turnout01,
    Turnout02,
    Turnout03,
    Turnout04,
    Turnout05,
    Turnout06,
}

public enum RedLightPosition
{
    下行正线,
    下行入库,
    上行,
    折返线1道,
    折返线2道
}

public enum IsPowerCut
{
    已断电,
    未断电
}

public enum RouteNodeTag
{
    道岔,
    红闪灯,
    钩锁器,
    断电
}

public enum LineName
{
    一号线,
}

public enum Line1StationName
{
    东环南路站
}

public enum LocomotionMode
{
    挥动,
    传送,
}

public enum ProjectName
{
    人工准备进路
}