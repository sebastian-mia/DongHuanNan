using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ReturnMenuUI : MonoBehaviour, IController
{
    public Button returnButton;
    public Button closeButton;

    private void OnEnable()
    {
        returnButton.onClick.AddListener(ReturnMenuScene);
        closeButton.onClick.AddListener(CloseReturnMenuScene);
    }

    private void ReturnMenuScene()
    {
        this.GetModel<TurnoutModel>().ResetValues();
        this.GetModel<GameRecordModel>().ResetValues();
        VRSceneManager.Instance.SwitchSceneAsync("MenuScene");
    }

    private void CloseReturnMenuScene()
    {
        UIManager.Instance.HideUI(this.gameObject);
    }

    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}