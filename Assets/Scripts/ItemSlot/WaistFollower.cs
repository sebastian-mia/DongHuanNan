using UnityEngine;

[AddComponentMenu("XR/OnlyRealBodyTurn")]
public class OnlyRealBodyTurn : MonoBehaviour
{
    [Header("转身检测")]
    public float turnDeadZone = 0.08f;
    [Range(1f, 50f)]
    public float rotationSmoothing = 15f;

    [Header("性能优化")]
    public bool useFixedUpdate = false;
    public int skipFrames = 0;

    [Header("调试")]
    public bool drawGizmo = true;

    private Transform camT;
    private Vector3 lastHeadPos;
    private float bodyYRot;
    private float lastBodyYRot;
    
    private Vector3 localOffset;
    private Quaternion localRotOffset;
    private int frameCounter;

    void Awake()
    {
        var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        camT = origin ? origin.Camera.transform : Camera.main.transform;
        
        Vector3 initialPos = camT.position;
        lastHeadPos = initialPos;
        bodyYRot = GetBodyYaw(camT.rotation); // 修正：移除 isVertical 参数
        lastBodyYRot = bodyYRot;

        Quaternion bodyRot = Quaternion.Euler(0, bodyYRot, 0);
        Quaternion invBody = Quaternion.Inverse(bodyRot);
        localOffset = invBody * (transform.position - initialPos);
        localRotOffset = invBody * transform.rotation;
    }

    void Update()
    {
        if (useFixedUpdate) return;
        DoUpdate();
    }

    void FixedUpdate()
    {
        if (!useFixedUpdate) return;
        DoUpdate();
    }

    void DoUpdate()
    {
        frameCounter++;
        if (skipFrames > 0 && frameCounter % (skipFrames + 1) != 0) return;

        Vector3 headPos = camT.position;
        Vector3 headFlatVel = (Vector3.ProjectOnPlane(headPos - lastHeadPos, Vector3.up)) / Time.deltaTime;
        lastHeadPos = headPos;

        // ========== 关键修正：始终计算 Yaw，不管是否垂直 ==========
        float currentYaw = GetBodyYaw(camT.rotation);
        
        // 身体旋转检测（始终更新，不判断垂直）
        if (headFlatVel.magnitude < turnDeadZone)
        {
            bodyYRot = currentYaw;
        }

        Quaternion bodyRot = Quaternion.Euler(0, bodyYRot, 0);

        // 位置实时跟随
        transform.position = headPos + bodyRot * localOffset;
        
        // 旋转更新
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(bodyYRot, lastBodyYRot));
        if (angleDiff > 0.001f)
        {
            Quaternion targetRotation = bodyRot * localRotOffset;
            
            float t = 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
            
            lastBodyYRot = bodyYRot;
        }
    }

    // ========== 关键修正：始终返回有效 Yaw，处理低头情况 ==========
    private float GetBodyYaw(Quaternion rotation)
    {
        Vector3 forward = rotation * Vector3.forward;
        float verticality = Mathf.Abs(forward.y);
        
        // 如果相机接近水平（正常站立），使用 forward
        if (verticality < 0.866f) // 小于 60 度
        {
            forward.y = 0;
            forward.Normalize();
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }
        
        // ========== 关键：低头/抬头时，使用 up 向量计算朝向 ==========
        Vector3 up = rotation * Vector3.up;
        up.y = 0;
        
        if (up.sqrMagnitude > 0.001f)
        {
            up.Normalize();
            // up 向量的 x/z 平面投影可以反映身体旋转
            // 需要 +90 度或 -90 度偏移，因为 up 垂直于 forward
            float yawFromUp = Mathf.Atan2(up.x, up.z) * Mathf.Rad2Deg;
            
            // 根据低头还是抬头，决定偏移方向
            if (forward.y > 0) // 抬头
                return yawFromUp + 180f;
            else // 低头
                return yawFromUp;
        }
        
        // 完全垂直（理论上不会发生），保持原值
        return bodyYRot;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.1f);
    }
}