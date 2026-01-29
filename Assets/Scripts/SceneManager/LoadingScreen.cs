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

    [Header("位置设置")] 
    [SerializeField] private float distance = 5f; // 前方 5m
    [SerializeField] private float heightOffset = 2f; // Y轴相对偏移 2m

    private float currentDisplayProgress = 0f;
    private float targetProgress = 0f;
    private bool isVisible = false;
    private Transform currentTarget; // 当前跟随的target

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

        // 【事件订阅】订阅XROrigin变化事件
        VRSceneManager.OnXROriginChanged += OnXROriginChanged;
        
        // 初始查找
        FindAndSetTarget();
    }

    void OnDestroy()
    {
        // 【事件取消订阅】防止内存泄漏
        VRSceneManager.OnXROriginChanged -= OnXROriginChanged;
    }

    // 【事件处理】XROrigin变化时更新位置
    private void OnXROriginChanged(Transform newXROrigin)
    {
        if (newXROrigin != null)
        {
            currentTarget = newXROrigin;
            Debug.Log($"[LoadingScreen] 收到XROrigin变化事件: {newXROrigin.name}");
            
            // 如果正在显示，立即更新位置
            if (isVisible)
            {
                UpdatePosition();
            }
        }
    }

    void FindAndSetTarget()
    {
        if (currentTarget == null)
        {
            var xro = FindObjectOfType<XROrigin>();
            if (xro != null)
                currentTarget = xro.transform;
        }
    }

    void Update()
    {
        if (!isVisible) return;

        // 每帧更新位置（确保跟随XROrigin移动）
        if (currentTarget != null)
        {
            UpdatePosition();
        }

        if (Mathf.Abs(currentDisplayProgress - targetProgress) > 0.001f)
        {
            currentDisplayProgress = Mathf.Lerp(currentDisplayProgress, targetProgress, Time.deltaTime * smoothSpeed);
            UpdateVisuals(currentDisplayProgress);
        }
    }

    private void UpdatePosition()
    {
        if (currentTarget == null) return;

        // 位置：前方 2m，高度相对 +2m
        Vector3 targetPosition = currentTarget.position + currentTarget.forward * distance;
        targetPosition.y = currentTarget.position.y + heightOffset;
        transform.position = targetPosition;

        // 面向玩家
        Vector3 direction = transform.position - currentTarget.position;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
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

        // 确保有target
        FindAndSetTarget();

        // 立即更新位置
        if (currentTarget != null)
        {
            UpdatePosition();
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