using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
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
    private GameObject targetRoute;

    private readonly string scorePanelName = "ScorePanel";
    private string currentRouteName;
    private int randomNum;

    void Awake()
    {
        turnoutModel = this.GetModel<TurnoutModel>();
        CreateTargetRoute();
        UIManager.Instance.ScanCanvas();
    }

    void Start()
    {
        this.RegisterEvent<ShowScorePanelEvent>(e =>
        {
            UIManager.Instance.HideUI(targetRoute);
            var scorePanel = UIManager.Instance.ShowUI(scorePanelName);
            var turnoutScoreUI = scorePanel.GetComponent<TrunoutScoreUI>();
            if (turnoutScoreUI != null)
            {
                turnoutScoreUI.projectName.text = currentRouteName;
                turnoutScoreUI.ShowNodeValue();
            }

            if (turnoutModel.isCorrectRoute)
            {
                ShowRoute();
            }
        });
    }

    private void CreateTargetRoute()
    {
        // randomNum = this.GetUtility<RandomNumber>().Range(1, this.GetModel<TurnoutModel>().routeCount);
        randomNum = 6;
        targetRoute = UIManager.Instance.ShowUI(targetRouteTip);
        if (randomNum == 1)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:东环南路下行至盛莫路下行进路";
            currentRouteName = targetRoute.GetComponent<TextMeshProUGUI>().text;
            this.SendCommand<PerpareDownSideToDownSideRouteCommand>();
        }

        if (randomNum == 2)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:东环南路下行至折1道进路";
            currentRouteName = targetRoute.GetComponent<TextMeshProUGUI>().text;
            this.SendCommand<PerpareDownSideToTranslate1RouteCommand>();
        }

        if (randomNum == 3)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:东环南路下行至折2道进路";
            currentRouteName = targetRoute.GetComponent<TextMeshProUGUI>().text;
            this.SendCommand<PerpareDownSideToTranslate2RouteCommand>();
        }

        if (randomNum == 4)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:折1道至东环南路下行进路";
            currentRouteName = targetRoute.GetComponent<TextMeshProUGUI>().text;
            this.SendCommand<PerpareTranslate1ToDownSideRouteCommand>();
        }

        if (randomNum == 5)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:折2道至东环南路下行进路";
            currentRouteName = targetRoute.GetComponent<TextMeshProUGUI>().text;
            this.SendCommand<PerpareTranslate2ToDownSideRouteCommand>();
        }

        if (randomNum == 6)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:东环南路上行至邱隘东上行进路";
            currentRouteName = targetRoute.GetComponent<TextMeshProUGUI>().text;
            this.SendCommand<PerpareUpSideToUpSideRouteCommand>();
        }

        if (randomNum == 7)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:东环南路上行至折1道进路";
            currentRouteName = targetRoute.GetComponent<TextMeshProUGUI>().text;
            this.SendCommand<PerpareUpSideToTranslate1RouteCommand>();
        }

        if (randomNum == 8)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:东环南路上行至折2道进路";
            currentRouteName = targetRoute.GetComponent<TextMeshProUGUI>().text;
            this.SendCommand<PerpareUpSideToTranslate2RouteCommand>();
        }

        if (randomNum == 9)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:折2道至东环南路下行进路";
            currentRouteName = targetRoute.GetComponent<TextMeshProUGUI>().text;
            this.SendCommand<PerpareTranslate2ToDownSideRouteCommand>();
        }

        if (randomNum == 10)
        {
            targetRoute.GetComponent<TextMeshProUGUI>().text = "请准备:折2道至东环南路上行进路";
            currentRouteName = targetRoute.GetComponent<TextMeshProUGUI>().text;
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