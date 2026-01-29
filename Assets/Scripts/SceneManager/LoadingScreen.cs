using UnityEngine;
using DG.Tweening;
using TMPro;
using Unity.XR.CoreUtils;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI References")] [SerializeField]
    private RectTransform container;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private RectTransform progressBar;
    [SerializeField] private float maxBarWidth = 400f;
    [SerializeField] private float smoothSpeed = 5f;

    [Header("位置设置")] [SerializeField] private Transform target; // XROrigin 引用
    [SerializeField] private float distance = 2f; // 前方 2m
    [SerializeField] private float heightOffset = 2f; // Y轴相对偏移 2m

    private float currentDisplayProgress = 0f;
    private float targetProgress = 0f;
    private bool isVisible = false;

    void Awake()
    {
        // 自动查找组件
        if (container == null)
        {
            Transform containerTrans = transform.Find("Container");
            if (containerTrans == null && transform.childCount > 0)
                containerTrans = transform.GetChild(0);
            if (containerTrans != null)
                container = containerTrans.GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (progressBar == null && container != null)
        {
            Transform barTrans = container.Find("ProgressBarBg/ProgressBar")
                                 ?? container.Find("ProgressBar");
            if (barTrans != null)
            {
                progressBar = barTrans.GetComponent<RectTransform>();
                maxBarWidth = progressBar.sizeDelta.x > 0 ? progressBar.sizeDelta.x : maxBarWidth;
            }
        }

        if (progressText == null)
            progressText = GetComponentInChildren<TextMeshProUGUI>(true);

        FindTarget();
    }

    void FindTarget()
    {
        if (target == null)
        {
            var xro = FindObjectOfType<XROrigin>();
            if (xro != null)
                target = xro.transform;
            else
                Debug.LogWarning("LoadingScreen: 未找到 XROrigin");
        }
    }

    void Update()
    {
        // 只在可见时更新进度条动画（位置不每帧更新）
        if (!isVisible) return;

        if (Mathf.Abs(currentDisplayProgress - targetProgress) > 0.001f)
        {
            currentDisplayProgress = Mathf.Lerp(currentDisplayProgress, targetProgress, Time.deltaTime * smoothSpeed);
            UpdateVisuals(currentDisplayProgress);
        }
    }

    public void SetProgress(float progress01)
    {
        targetProgress = Mathf.Clamp01(progress01);
    }

    private void UpdateVisuals(float progress)
    {
        if (progressBar != null)
        {
            float width = progress * maxBarWidth;
            progressBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        if (progressText != null)
        {
            if (progress >= 0.89f && progress < 0.99f)
                progressText.text = "加载中";
            else if (progress >= 0.99f)
                progressText.text = "加载完成";
            else
                progressText.text = $"{(progress * 100):0}%";
        }
    }

    public void ShowImmediate()
    {
        gameObject.SetActive(true);
        isVisible = true;

        FindTarget();

        if (target != null)
        {
            // 位置：前方 2m，高度相对 +2m
            Vector3 targetPosition = target.position + target.forward * distance;
            targetPosition.y = target.position.y + heightOffset;
            transform.position = targetPosition;

            // 【修正背向问题】计算从 XROrigin 指向 UI 的方向（即 UI 应该面向的反向）
            Vector3 direction = transform.position - target.position; // 从玩家指向UI
            direction.y = 0; // 保持水平

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                // 只取 Y 轴，确保 UI 平面正对玩家
                transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
            }
        }

        // 直接显示（无淡入动画）
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (container != null)
            container.localScale = Vector3.one;

        // 重置进度
        currentDisplayProgress = 0f;
        targetProgress = 0f;
        UpdateVisuals(0f);
    }

    /// <summary>
    /// 直接隐藏（无动画）
    /// </summary>
    public void HideImmediate()
    {
        isVisible = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }
}