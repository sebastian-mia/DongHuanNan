using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ReturnMenuUI : MonoBehaviour
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
        VRSceneManager.Instance.SwitchSceneAsync("MenuScene");
    }

    private void CloseReturnMenuScene()
    {
        UIManager.Instance.HideUI(this.gameObject);
    }
}