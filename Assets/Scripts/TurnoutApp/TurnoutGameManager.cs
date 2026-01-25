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
    private readonly string downSideToDownSideRouteShowName = "DownSideToDownSide";
    private readonly string downSideToTranslate1RouteShowName = "DownSideToTranslate1";
    private readonly string downSideToTranslate2RouteShowName = "DownSideToTranslate2";
    private readonly string translate1ToDownSideRouteShowName = "Translate1ToDownSide";
    private readonly string translate2ToDownSideRouteShowName = "Translate2ToDownSide";
    private readonly string upSideToTranslate1RouteShowName = "UpSideToTranslate1";
    private readonly string upSideToTranslate2RouteShowName = "UpSideToTranslate2";
    private readonly string translate1ToUpSideRouteShowName = "Translate1ToUpSide";
    private readonly string translate2ToUpSideRouteShowName = "Translate2ToUpSide";
    private readonly string upSideToUpSideRouteShowName = "UpSideToUpSide";
    private readonly string targetRouteTip = "TargetRouteTip";
    private int randomNum;

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
        UIManager.Instance.ScanCanvas();
    }

    private void CreateTargetRoute()
    {
        randomNum = this.GetUtility<RandomNumber>().Range(1, this.GetModel<TurnoutModel>().routeCount);
        var targetRoute = UIManager.Instance.ShowUI(targetRouteTip);
        if (randomNum == 1)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "东环南路下行至福庆北路下行进路";
            this.SendCommand<PerpareDownSideToDownSideRouteCommand>();
        }

        if (randomNum == 2)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "东环南路下行至折1道进路";
            this.SendCommand<PerpareDownSideToTranslate1RouteCommand>();
        }

        if (randomNum == 3)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "东环南路下行至折2道进路";
            this.SendCommand<PerpareDownSideToTranslate2RouteCommand>();
        }

        if (randomNum == 4)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "折1道至东环南路下行进路";
            this.SendCommand<PerpareTranslate1ToDownSideRouteCommand>();
        }

        if (randomNum == 5)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "折2道至东环南路下行进路";
            this.SendCommand<PerpareTranslate2ToDownSideRouteCommand>();
        }

        if (randomNum == 6)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "东环南路上行至邱隘东上行进路";
            this.SendCommand<PerpareUpSideToUpSideRouteCommand>();
        }

        if (randomNum == 7)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "东环南路上行至折1道进路";
            this.SendCommand<PerpareUpSideToTranslate1RouteCommand>();
        }

        if (randomNum == 8)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "东环南路上行至折2道进路";
            this.SendCommand<PerpareUpSideToTranslate2RouteCommand>();
        }

        if (randomNum == 9)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "折2道至东环南路下行进路";
            this.SendCommand<PerpareTranslate2ToDownSideRouteCommand>();
        }

        if (randomNum == 10)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "折2道至东环南路上行进路";
            this.SendCommand<PerpareTranslate2ToUpSideRouteCommand>();
        }
    }

    private void ShowRoute()
    {
        if (randomNum == 1)
        {
            UIManager.Instance.ShowUI(downSideToDownSideRouteShowName);
        }

        if (randomNum == 2)
        {
            UIManager.Instance.ShowUI(downSideToTranslate1RouteShowName);
        }

        if (randomNum == 3)
        {
            UIManager.Instance.ShowUI(downSideToTranslate2RouteShowName);
        }

        if (randomNum == 4)
        {
            UIManager.Instance.ShowUI(translate1ToDownSideRouteShowName);
        }

        if (randomNum == 5)
        {
            UIManager.Instance.ShowUI(translate1ToUpSideRouteShowName);
        }

        if (randomNum == 6)
        {
            UIManager.Instance.ShowUI(upSideToUpSideRouteShowName);
        }

        if (randomNum == 7)
        {
            UIManager.Instance.ShowUI(upSideToTranslate1RouteShowName);
        }

        if (randomNum == 8)
        {
            UIManager.Instance.ShowUI(upSideToTranslate2RouteShowName);
        }

        if (randomNum == 9)
        {
            UIManager.Instance.ShowUI(translate2ToDownSideRouteShowName);
        }

        if (randomNum == 10)
        {
            UIManager.Instance.ShowUI(translate2ToUpSideRouteShowName);
        }
    }

    private void CaculateScore()
    {
    }

    void Update()
    {
        // var currents = this.GetModel<TurnoutModel>().currentRoute.Value.turnoutMachines;
        // var targets = this.GetModel<TurnoutModel>().targetRoute.Value.turnoutMachines;
        // foreach (var current in currents)
        // {
        //     Debug.Log("Current TtunoutId:" + current.Value.TurnoutID + "TurnoutState" + current.Value.TurnoutState);
        // }
        //
        // foreach (var target in targets)
        // {
        //     Debug.Log("Target TtunoutId:" + target.Value.TurnoutID + "TurnoutState" + target.Value.TurnoutState);
        // }
    }

    public IArchitecture GetArchitecture()
    {
        return TurnoutApp.Interface;
    }
}