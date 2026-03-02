using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserIdPanel : MonoBehaviour
{
    public TMP_InputField userId;
    public Button confirmButton;
    public GameObject tip;
    public Button returnButton;

    private void OnEnable()
    {
        UITweenAnimation.ScaleIn(this.gameObject);
    }
}