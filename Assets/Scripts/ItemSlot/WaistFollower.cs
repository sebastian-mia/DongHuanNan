using UnityEngine;

/// <summary>
/// 只有现实身体旋转（脚转）才影响物体旋转，纯转头不转。
/// </summary>
[AddComponentMenu("XR/OnlyRealBodyTurn")]
public class OnlyRealBodyTurn : MonoBehaviour
{
    [Header("转身检测")]
    public float turnDeadZone = 0.08f;   // 头水平速度低于此值认为是身体在转

    [Header("调试")]
    public bool drawGizmo = true;

    private Transform camT;
    private Vector3 lastHeadPos;
    private float bodyYRot;              // 当前身体 Y 朝向

    private Vector3 localOffset;         // 初始水平偏移
    private Quaternion localRotOffset;   // 初始旋转偏移

    void Awake()
    {
        var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        camT = origin ? origin.Camera.transform : Camera.main.transform;
        lastHeadPos = camT.position;
        bodyYRot = camT.eulerAngles.y;

        // 记录初始相对差（身体坐标系）
        Quaternion bodyRot = Quaternion.Euler(0, bodyYRot, 0);
        Quaternion invBody = Quaternion.Inverse(bodyRot);

        localOffset   = invBody * (transform.position - camT.position);
        localRotOffset = invBody * transform.rotation;
    }

    void LateUpdate()
    {
        Vector3 headPos = camT.position;
        Vector3 headFlatVel = (Vector3.ProjectOnPlane(headPos - lastHeadPos, Vector3.up)) / Time.deltaTime;
        lastHeadPos = headPos;

        // 死区：原地转身才更新身体朝向
        if (headFlatVel.magnitude < turnDeadZone)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(camT.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude > 0.0001f)
                bodyYRot = Quaternion.LookRotation(flatForward, Vector3.up).eulerAngles.y;
        }

        Quaternion bodyRot = Quaternion.Euler(0, bodyYRot, 0);

        // 世界坐标 = 当前身体原点 + 水平旋转 * 初始偏移
        transform.position = camT.position + bodyRot * localOffset;
        transform.rotation = bodyRot * localRotOffset;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.1f);
    }
}