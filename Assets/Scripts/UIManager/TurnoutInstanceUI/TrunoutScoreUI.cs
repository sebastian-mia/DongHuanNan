using System;
using System.Collections;
using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class TrunoutScoreUI : MonoBehaviour, IController
{
    public TMP_Text projectName;
    private Transform content;
    private TurnoutModel turnoutModel;
    private string valuePanelName = "ValuePanel";
    public Button restartButton;

    void Start()
    {
        restartButton.onClick.AddListener(ReturnMenuScene);
        content = transform.GetComponent<ScrollRect>().content;
    }

    private void OnEnable()
    {
        turnoutModel = this.GetModel<TurnoutModel>();
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
                valuePanel.GetComponent<NodeValueUI>().faultImage.gameObject.SetActive(false);
                valuePanel.GetComponent<NodeValueUI>().correntImage.gameObject.SetActive(true);
            }
            else
            {
                valuePanel.GetComponent<NodeValueUI>().faultImage.gameObject.SetActive(true);
                valuePanel.GetComponent<NodeValueUI>().correntImage.gameObject.SetActive(false);
            }
        }
    }

    private void ReturnMenuScene()
    {
        VRSceneManager.Instance.SwitchSceneAsync("MenuScene");
    }

    public IArchitecture GetArchitecture()
    {
        return TurnoutApp.Interface;
    }
}