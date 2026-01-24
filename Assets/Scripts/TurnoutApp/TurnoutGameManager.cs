using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using TMPro;
using UnityEngine.UI;

public class TurnoutGameManager : MonoBehaviour, IController
{
    private TurnoutModel turnoutModel;
    private int correctCount = 0;

    //RouteShow
    public Transform downSideToDownSideRouteShow;
    public Transform downSideToTranslate1RouteShow;
    public Transform downSideToTranslate2RouteShow;
    public Transform translate1ToDownSideRouteShow;
    public Transform translate2ToDownSideRouteShow;
    public Transform upSideToTranslate1RouteShow;
    public Transform upSideToTranslate2RouteShow;
    public Transform translate1ToUpSideRouteShow;
    public Transform translate2ToUpSideRouteShow;
    public Transform upSideToUpSideRouteShow;
    private int randomNum;

    //RouteTip
    public TextMeshProUGUI targetRouteTip;

    void Awake()
    {
        turnoutModel = this.GetModel<TurnoutModel>();
        CreateTargetRoute();
        turnoutModel.isCorrectRoute.Register((value) =>
        {
            if (value)
            {
                ShowRoute();
            }
        });
    }

    private void CreateTargetRoute()
    {
        randomNum = this.GetUtility<RandomNumber>().Range(1, this.GetModel<TurnoutModel>().routeCount);
        if (randomNum == 1)
        {
            targetRouteTip.text = "东环南路下行至福庆北路下行进路";
            this.SendCommand<PerpareDownSideToDownSideRouteCommand>();
        }

        if (randomNum == 2)
        {
            targetRouteTip.text = "东环南路下行至折1道进路";
            this.SendCommand<PerpareDownSideToTranslate1RouteCommand>();
        }

        if (randomNum == 3)
        {
            targetRouteTip.text = "东环南路下行至折2道进路";
            this.SendCommand<PerpareDownSideToTranslate2RouteCommand>();
        }

        if (randomNum == 4)
        {
            targetRouteTip.text = "折1道至东环南路下行进路";
            this.SendCommand<PerpareTranslate1ToDownSideRouteCommand>();
        }

        if (randomNum == 5)
        {
            targetRouteTip.text = "折2道至东环南路下行进路";
            this.SendCommand<PerpareTranslate2ToDownSideRouteCommand>();
        }

        if (randomNum == 6)
        {
            targetRouteTip.text = "东环南路上行至邱隘东上行进路";
            this.SendCommand<PerpareUpSideToUpSideRouteCommand>();
        }

        if (randomNum == 7)
        {
            targetRouteTip.text = "东环南路上行至折1道进路";
            this.SendCommand<PerpareUpSideToTranslate1RouteCommand>();
        }

        if (randomNum == 8)
        {
            targetRouteTip.text = "东环南路上行至折2道进路";
            this.SendCommand<PerpareUpSideToTranslate2RouteCommand>();
        }

        if (randomNum == 9)
        {
            targetRouteTip.text = "折2道至东环南路下行进路";
            this.SendCommand<PerpareTranslate2ToDownSideRouteCommand>();
        }

        if (randomNum == 10)
        {
            targetRouteTip.text = "折2道至东环南路上行进路";
            this.SendCommand<PerpareTranslate2ToUpSideRouteCommand>();
        }
    }

    private void ShowRoute()
    {
        if (randomNum == 1)
        {
            downSideToDownSideRouteShow.gameObject.SetActive(true);
        }

        if (randomNum == 2)
        {
            downSideToTranslate1RouteShow.gameObject.SetActive(true);
        }

        if (randomNum == 3)
        {
            downSideToTranslate2RouteShow.gameObject.SetActive(true);
        }

        if (randomNum == 4)
        {
            translate1ToDownSideRouteShow.gameObject.SetActive(true);
        }

        if (randomNum == 5)
        {
            translate1ToUpSideRouteShow.gameObject.SetActive(true);
        }

        if (randomNum == 6)
        {
            upSideToUpSideRouteShow.gameObject.SetActive(true);
        }

        if (randomNum == 7)
        {
            upSideToTranslate1RouteShow.gameObject.SetActive(true);
        }

        if (randomNum == 8)
        {
            upSideToTranslate2RouteShow.gameObject.SetActive(true);
        }

        if (randomNum == 9)
        {
            translate2ToDownSideRouteShow.gameObject.SetActive(true);
        }

        if (randomNum == 10)
        {
            translate2ToUpSideRouteShow.gameObject.SetActive(true);
        }
    }

    private void CaculateScore()
    {
    }

    void Update()
    {
        var currents = this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines;
        var targets = this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines;
        foreach (var current in currents)
        {
            Debug.Log("Current TtunoutId:" + current.Value.TurnoutID + "TurnoutState" + current.Value.TurnoutState);
        }

        foreach (var target in targets)
        {
            Debug.Log("Target TtunoutId:" + target.Value.TurnoutID + "TurnoutState" + target.Value.TurnoutState);
        }
    }

    public IArchitecture GetArchitecture()
    {
        return TurnoutApp.Interface;
    }
}