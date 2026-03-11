using System;
using System.Collections;
using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

public class TrunoutScoreUI : MonoBehaviour, IController
{
    public TMP_Text subProjectName;
    public TMP_Text counterTime;
    private Transform content;
    private TurnoutModel turnoutModel;
    public string valuePanelName = "ValuePanel";
    public Button returnButton;
    public Button returnPerUIButton;
    private TurnoutDatabaseManager turnoutDatabaseManager;
    private TurnoutRecordModel turnoutRecordModel;
    private GameRecordModel gameRecordModel;
    private int projectId;
    private AudioManager audioManager;

    void Start()
    {
        audioManager = this.GetUtility<AudioManager>();
    }

    private void OnEnable()
    {
        if (returnButton != null)
        {
            returnButton.onClick.AddListener(ReturnMenuScene);
        }

        if (returnPerUIButton != null)
        {
            returnPerUIButton.onClick.AddListener(ReturnPrePanel);
        }

        content = transform.GetComponentInChildren<ScrollRect>().content;
        turnoutModel = this.GetModel<TurnoutModel>();
        turnoutRecordModel = this.GetModel<TurnoutRecordModel>();
        gameRecordModel = this.GetModel<GameRecordModel>();
        turnoutDatabaseManager = this.GetUtility<TurnoutDatabaseManager>();
        var projectId = turnoutDatabaseManager.GetProjectId(ProjectName.人工准备进路.ToString());
        if (projectId.HasValue)
        {
            this.projectId = projectId.Value;
        }
    }

    private void CreateItem(Transform valuePanel)
    {
        // 关键：设置正确的局部坐标（UI 元素必须设置）
        RectTransform rectTransform = valuePanel.GetComponent<RectTransform>();

        // 重置锚点和坐标（避免位置错乱）
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
    }

    public void ShowNodeValue()
    {
        var turnoutSubProjectData = new TurnoutSubProjectData(projectId,
            turnoutModel.currentsubProjectName, gameRecordModel.UserId, turnoutModel.counterTime,
            turnoutModel.currentStationId);
        var turnoutNodes = new List<TurnoutNodeData>();
        for (var i = 0; i < turnoutModel.nodeValues.Count; i++)
        {
            var nodeValue = turnoutModel.nodeValues[i];
            var valuePanel = UIManager.Instance.ShowUI(valuePanelName);
            valuePanel.transform.SetParent(content);
            CreateItem(valuePanel.transform);
            valuePanel.GetComponent<NodeValueUI>().nodeName.text = nodeValue.nodeName;
            valuePanel.GetComponent<NodeValueUI>().targetValue.text = nodeValue.targetValue;
            valuePanel.GetComponent<NodeValueUI>().currentValue.text = nodeValue.currentValue;
            if (turnoutModel.nodeValues[i].isConsistence)
            {
                Debug.Log("corrent");
                valuePanel.GetComponent<NodeValueUI>().faultImage.gameObject.SetActive(false);
                valuePanel.GetComponent<NodeValueUI>().correntImage.gameObject.SetActive(true);
            }
            else
            {
                Debug.Log("fault");
                valuePanel.GetComponent<NodeValueUI>().faultImage.gameObject.SetActive(true);
                valuePanel.GetComponent<NodeValueUI>().correntImage.gameObject.SetActive(false);
            }

            var turnoutNodeData = new TurnoutNodeData(
                nodeValue.nodeName,
                nodeValue.targetValue, nodeValue.currentValue,
                nodeValue.isConsistence);
            turnoutNodes.Add(turnoutNodeData);
        }

        this.SendCommand<SetTurnoutSubProjectAndNodeDataCommand>(
            new SetTurnoutSubProjectAndNodeDataCommand(turnoutSubProjectData, turnoutNodes));
    }

    private void ReturnPrePanel()
    {
        UIManager.Instance.PopUI();
    }

    private void ReturnMenuScene()
    {
        if (audioManager != null)
        {
            audioManager.StopAllSounds();
        }

        for (int i = 0; i < content.childCount; i++)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        turnoutModel.ResetValues();
        gameRecordModel.ResetValues();
        VRSceneManager.Instance.SwitchSceneAsync("MenuScene");
    }


    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}