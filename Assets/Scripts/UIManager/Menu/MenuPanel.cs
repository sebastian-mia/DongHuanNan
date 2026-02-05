using System;
using UnityEngine;
using UnityEngine.UI;

public class MenuPanel : MonoBehaviour
{
    public Button startGameButton;
    public Button boardButton;
    public Button infoButton;
    public Button settingButton;
    public Button controlPracticeButton;


    private void OnEnable()
    {
        UITweenAnimation.ScaleIn(this.gameObject);
    }
}