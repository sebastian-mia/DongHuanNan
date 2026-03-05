using System;
using System.Collections;
using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserValuePanel : MonoBehaviour, IController
{
    public TMP_Text id;
    public TMP_Text userId;
    public TMP_Text counterTime;
    public TMP_Text time;
    public string subProjectName;
    public Button submitButton;
    private IController controllerImplementation;
    private TurnoutDatabaseManager databaseManager;
    private readonly string TurnoutProjectWithIDScorePanelTag = "TurnoutProjectWithIDScorePanel";

    void Start()
    {
        databaseManager = this.GetUtility<TurnoutDatabaseManager>();
        submitButton.onClick.AddListener(ShowTurnoutRecordScoreUI);
    }

    private void ShowTurnoutRecordScoreUI()
    {
        var trunoutRecordScoreUI = UIManager.Instance.PushUI(TurnoutProjectWithIDScorePanelTag);
        if (trunoutRecordScoreUI != null)
        {
            var turnoutRecordScore = trunoutRecordScoreUI.GetComponent<TurnoutProjectWithIDScorePanel>();
            if (turnoutRecordScore != null)
            {
                turnoutRecordScore.ShowTurnoutRecordScoreValue(subProjectName, userId.text,
                    counterTime.text, time.text);
            }
        }
    }

    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}