using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel : MonoBehaviour
{
    public Button returnButton;

    void Start()
    {
        returnButton.onClick.AddListener(ReturnPrePanel);
    }

    private void OnEnable()
    {
        UITweenAnimation.ScaleIn(this.gameObject);
    }

    private void ReturnPrePanel()
    {
        UIManager.Instance.PopUI();
    }
}