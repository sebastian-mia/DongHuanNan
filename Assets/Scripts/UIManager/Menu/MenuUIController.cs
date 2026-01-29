using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    public Button startGameBtn;
    public Button settingBtn;

    void Start()
    {
        startGameBtn.onClick.AddListener(StartGame);
    }

    private void StartGame()
    {
        VRSceneManager.Instance.SwitchSceneAsync("DongHuanNan");
    }
}