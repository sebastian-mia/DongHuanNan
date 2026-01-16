using UnityEngine;

public class FollowForceItem : MonoBehaviour
{
    [Header("必须拖入 ForceItem")]
    public Transform forceRoot;

    [Header("仅勾选时移动一半距离")]
    public bool moveHalf = true;

    private float lastParentX;
    private bool  warned  = false;

    void OnEnable()
    {
        if (!forceRoot)
        {
            Debug.LogError("[FollowForceItem] forceRoot 未赋值，脚本无效！", this);
            warned = true;
            enabled = false;
            return;
        }
        lastParentX = forceRoot.position.x;
    }

    void Update()
    {
        if (!forceRoot) return;

        float currX = forceRoot.position.x;
        float delta = currX - lastParentX;
        lastParentX = currX;

        float ratio   = moveHalf ? 0.5f : 1f;
        float moveX   = delta * ratio;

        /* 只沿本地 X 轴移动：直接改 localPosition 的 x 分量 */
        Vector3 lp = transform.localPosition;
        lp.x += moveX;
        transform.localPosition = lp;
    }
}