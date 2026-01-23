using UnityEngine;

/// <summary>
/// 仅在同父且 ForceItem 只改 localPosition 时适用
/// </summary>
public class FollowForceItemLocal : MonoBehaviour
{
    [Header("必须拖入 ForceItem")]
    public Transform forceRoot;

    [Header("仅勾选时移动一半距离")]
    public bool moveHalf = true;

    private Vector3 lastLocalPos;
    private bool    warned;

    void OnEnable()
    {
        if (!forceRoot)
        {
            Debug.LogError("[FollowForceItemLocal] forceRoot 未赋值，脚本无效！", this);
            warned = true;
            enabled = false;
            return;
        }

        // 记录 ForceItem 的初始局部坐标
        lastLocalPos = forceRoot.localPosition;
    }

    void LateUpdate()   // 用 LateUpdate 保证 ForceItem 先走完
    {
        if (!forceRoot) return;

        Vector3 currLocalPos = forceRoot.localPosition;
        Vector3 localDelta   = currLocalPos - lastLocalPos;

        float ratio = moveHalf ? 0.5f : 1f;
        transform.localPosition += localDelta * ratio;

        lastLocalPos = currLocalPos;
    }
}