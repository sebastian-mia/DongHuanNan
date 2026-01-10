// FollowHalfX.cs
using UnityEngine;

public class FollowForceItem : MonoBehaviour
{
    [Header("必须拖入 ForceItem")]
    public Transform forceRoot;   // 不再自动赋值

    [Header("仅勾选时移动一半距离")]
    public bool moveHalf = true;

    private float lastParentX;
    private bool warned = false;

    void OnEnable()
    {
        if (!forceRoot)
        {
            Debug.LogError("[FollowHalfX] forceRoot 未赋值，脚本无效！", this);
            warned = true;
            enabled = false;   // 直接停用，避免空跑
            return;
        }

        lastParentX = forceRoot.position.x;
    }

    void Update()
    {
        if (!forceRoot) return;          // 保险

        float currX = forceRoot.position.x;
        float delta = currX - lastParentX;
        lastParentX = currX;

        float ratio = moveHalf ? 0.5f : 1f;
        transform.position += new Vector3(delta * ratio, 0, 0);
    }
}