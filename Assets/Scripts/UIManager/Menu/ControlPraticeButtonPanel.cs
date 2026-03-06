using UnityEngine;
using UnityEngine.UI;

public class ControlPraticeButtonPanel : MonoBehaviour
{
    [Header("对象列表")] public GameObject[] objects = new GameObject[4]; // 4个对象

    [Header("UI按钮")] public Button prevButton; // 前一个按钮
    public Button nextButton; // 后一个按钮

    [Header("可选：显示当前索引")] public Text indexText; // 显示 1/4

    private int currentIndex = 0; // 当前显示的对象索引
    public Button returnButton;

    void Start()
    {
        // 绑定按钮事件
        prevButton.onClick.AddListener(ShowPrevious);
        nextButton.onClick.AddListener(ShowNext);
        returnButton.onClick.AddListener(ReturnPrePanel);

        // 初始化显示
        UpdateView();
    }

    private void OnEnable()
    {
        UITweenAnimation.ScaleIn(this.gameObject);
        currentIndex = 0;
        UpdateView();
    }

    /// <summary>
    /// 显示前一个（循环）
    /// </summary>
    public void ShowPrevious()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = objects.Length - 1; // 循环到最后一个

        UpdateView();
    }

    /// <summary>
    /// 显示后一个（循环）
    /// </summary>
    public void ShowNext()
    {
        currentIndex++;
        if (currentIndex >= objects.Length)
            currentIndex = 0; // 循环到第一个

        UpdateView();
    }

    /// <summary>
    /// 更新显示状态
    /// </summary>
    void UpdateView()
    {
        // 隐藏所有
        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(false);
        }

        // 显示当前
        if (objects[currentIndex] != null)
            objects[currentIndex].SetActive(true);

        // 更新UI文本
        if (indexText != null)
            indexText.text = $"{currentIndex + 1} / {objects.Length}";

        Debug.Log($"当前显示: {currentIndex + 1} / {objects.Length}");
    }


    private void ReturnPrePanel()
    {
        UIManager.Instance.PopUI();
    }

    void OnDestroy()
    {
        // 移除事件监听
        prevButton.onClick.RemoveListener(ShowPrevious);
        nextButton.onClick.RemoveListener(ShowNext);
    }
}