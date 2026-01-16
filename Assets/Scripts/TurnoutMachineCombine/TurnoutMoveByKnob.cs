    using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class TurnoutMoveTByKnob : MonoBehaviour
{
    [Header("移动设置")] [Tooltip("最大伸出距离（米）")]
    public float maxDistance = 0.5f;

    [Tooltip("移动方向（本地坐标系）")] public Vector3 moveAxis = Vector3.right;

    [Tooltip("是否使用世界坐标系")] public bool useWorldSpace = false;

    [Header("关联组件")] public XRKnob knob;

    [Header("调试")] public bool showDebug = true;

    private Vector3 basePos; // 基准位置
    private float lastReportedValue = -1f;

    void Awake()
    {
        // 获取基准位置
        basePos = useWorldSpace ? transform.position : transform.localPosition;

        // 自动获取组件
        if (knob == null)
            knob = GetComponentInChildren<XRKnob>();

        // 验证配置
        ValidateConfiguration();

        // 初始同步
        UpdatePositionFromKnob();
    }

    void Update()
    {
        // 只根据knob的value更新位置
        UpdatePositionFromKnob();
    }

    void ValidateConfiguration()
    {
        if (knob == null)
        {
            // Debug.LogError("TurnoutMoveByKnob: 未找到 XRKnob 组件！");
            return;
        }

        // 确保钳制启用
        if (!knob.clampedMotion)
        {
            Debug.LogWarning("TurnoutMoveByKnob: XRKnob.clampedMotion 为 false，已自动启用！");
            knob.clampedMotion = true;
        }

        // 确保 value 在合理范围
        if (knob.value < 0f || knob.value > 1f)
        {
            Debug.LogWarning($"TurnoutMoveByKnob: knob.value({knob.value}) 超出 [0,1] 范围，已修正！");
            knob.value = Mathf.Clamp01(knob.value);
        }

        // 角度范围验证
        if (Mathf.Abs(knob.maxAngle - knob.minAngle) < 0.1f)
        {
            Debug.LogWarning($"TurnoutMoveByKnob: 角度范围过小 ({knob.minAngle} -> {knob.maxAngle})");
        }
    }

    /// <summary>
    /// 根据knob的value更新位置
    /// </summary>
    void UpdatePositionFromKnob()
    {
        if (knob == null) return;

        // 强制钳制 value 到 [0,1]
        float clampedValue = Mathf.Clamp01(knob.value);

        // 如果 value 被钳制了，同步回 knob
        if (Mathf.Abs(clampedValue - knob.value) > 0.001f)
        {
            knob.value = clampedValue;

            if (showDebug)
                Debug.LogWarning($"强制钳制 value: {knob.value} -> {clampedValue}");
        }

        // 计算位移
        float distance = clampedValue * maxDistance;
        Vector3 movement = moveAxis.normalized * distance;

        // 应用位移
        if (useWorldSpace)
        {
            transform.position = basePos + movement;
        }
        else
        {
            transform.localPosition = basePos + movement;
        }

        // 调试输出
        // if (showDebug && Mathf.Abs(clampedValue - lastReportedValue) > 0.001f)
        // {
        //     Debug.Log($"位置更新: value={clampedValue:F3}, distance={distance:F3}m, knob实际value={knob.value:F3}");
        //     lastReportedValue = clampedValue;
        // }
    }

    /// <summary>
    /// 获取当前移动距离百分比 (0-1)
    /// </summary>
    public float GetNormalizedDistance()
    {
        if (knob == null) return 0f;
        return Mathf.Clamp01(knob.value);
    }

    /// <summary>
    /// 获取当前实际移动距离
    /// </summary>
    public float GetCurrentDistance()
    {
        return GetNormalizedDistance() * maxDistance;
    }

    /// <summary>
    /// 设置移动百分比 (0-1)
    /// </summary>
    public void SetNormalizedDistance(float normalizedDistance)
    {
        if (knob == null) return;

        float clamped = Mathf.Clamp01(normalizedDistance);
        knob.value = clamped;
        UpdatePositionFromKnob();
    }

    /// <summary>
    /// 设置基准位置为当前位置
    /// </summary>
    public void ResetBasePosition()
    {
        basePos = useWorldSpace ? transform.position : transform.localPosition;
        Debug.Log($"重置基准位置: {basePos}");
    }

    /// <summary>
    /// 设置最大移动距离
    /// </summary>
    public void SetMaxDistance(float distance)
    {
        maxDistance = Mathf.Max(0.01f, distance);

        // 保持当前位置比例不变
        float currentPercent = GetNormalizedDistance();
        UpdatePositionFromKnob();
    }

    void OnDrawGizmosSelected()
    {
        Vector3 basePosition;

        // 确定基准位置
        if (Application.isPlaying)
        {
            basePosition = useWorldSpace
                ? basePos
                : (transform.parent != null ? transform.parent.TransformPoint(basePos) : basePos);
        }
        else
        {
            basePosition = useWorldSpace
                ? transform.position
                : (transform.parent != null
                    ? transform.parent.InverseTransformPoint(transform.position)
                    : transform.position);
        }

        // 绘制移动轴
        Gizmos.color = Color.blue;
        Vector3 startPos = basePosition;
        Vector3 endPos = startPos + moveAxis.normalized * maxDistance;

        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawSphere(startPos, 0.02f);
        Gizmos.DrawSphere(endPos, 0.02f);

        // 绘制当前位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.03f);

        // 绘制百分比标记
        if (knob != null)
        {
            float percent = knob.value;
            Vector3 currentPos = startPos + moveAxis.normalized * (maxDistance * percent);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentPos, 0.025f);

            // 文本标签
#if UNITY_EDITOR
            UnityEditor.Handles.Label(currentPos + Vector3.up * 0.05f, $"{percent:P0}");
#endif
        }
    }
}