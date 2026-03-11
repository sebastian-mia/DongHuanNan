using System;
using System.Collections;
using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

public class TurnoutProjectWithIDScorePanel : MonoBehaviour, IController
{
    public TMP_Text projectName;
    public TMP_Text counterTimeText;
    private Transform content;
    public string valuePanelName = "RecordValuePanel";
    public Button returnPerUIButton;
    private TurnoutDatabaseManager turnoutDatabaseManager;
    private int ButtonIndex;
    private List<TurnoutNodeData> turnoutNodeDatas;
    private GameObject showMessageTag;

    private void OnEnable()
    {
        turnoutDatabaseManager = this.GetUtility<TurnoutDatabaseManager>();
        if (ButtonIndex < 1)
        {
            returnPerUIButton.onClick.AddListener(ReturnPrePanel);
            ButtonIndex++;
        }

        content = transform.GetComponentInChildren<ScrollRect>().content;
    }

    public void ShowTurnoutRecordScoreValue(string subProjectName, string userId, string counterTime, string time)
    {
        projectName.text = subProjectName;
        counterTimeText.text = counterTime;
        turnoutNodeDatas = turnoutDatabaseManager.GetNodesBySubProject(subProjectName, userId, time);
        ShowNodeValue(turnoutNodeDatas);
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

    private void ShowNodeValue(List<TurnoutNodeData> turnoutNodeDatas)
    {
        for (var i = 0; i < turnoutNodeDatas.Count; i++)
        {
            var nodeValue = turnoutNodeDatas[i];
            var valuePanel = UIManager.Instance.ShowUI(valuePanelName);
            valuePanel.transform.SetParent(content);
            CreateItem(valuePanel.transform);
            valuePanel.GetComponent<NodeValueUI>().nodeName.text = nodeValue.NodeName;
            valuePanel.GetComponent<NodeValueUI>().targetValue.text = nodeValue.TargetState;
            valuePanel.GetComponent<NodeValueUI>().currentValue.text = nodeValue.CurrentState;
            if (nodeValue.IsConsistent)
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
        }
    }

    private void ReturnPrePanel()
    {
        for (int i = 0; i < content.childCount; i++)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        turnoutNodeDatas.Clear();

        UIManager.Instance.PopUI();
    }


    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}