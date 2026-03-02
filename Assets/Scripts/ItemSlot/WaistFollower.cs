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
    private float bodyYRot;      // 检测到的身体朝向（通过死区逻辑更新）
    private float smoothedBodyYRot; // 平滑后的实际旋转
    
    private Vector3 localOffset;
    private Quaternion localRotOffset;
    private int frameCounter;

    void Awake()
    {
        var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        camT = origin ? origin.Camera.transform : Camera.main.transform;
        
        Vector3 initialPos = camT.position;
        lastHeadPos = initialPos;
        bodyYRot = GetBodyYaw(camT.rotation);
        smoothedBodyYRot = bodyYRot;

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

        // ========== 身体旋转检测（死区逻辑） ==========
        float currentYaw = GetBodyYaw(camT.rotation);
        
        // 只有脚不动时，才更新身体朝向目标值
        if (headFlatVel.magnitude < turnDeadZone)
        {
            bodyYRot = currentYaw;
        }

        // ========== 关键修改：始终平滑跟随 bodyYRot（不管是否在死区） ==========
        float t = 1f - Mathf.Exp(-rotationSmoothing * Time.deltaTime);
        smoothedBodyYRot = Mathf.LerpAngle(smoothedBodyYRot, bodyYRot, t);
        
        Quaternion bodyRot = Quaternion.Euler(0, smoothedBodyYRot, 0);

        // ========== 位置实时跟随（保持相对偏移） ==========
        transform.position = headPos + bodyRot * localOffset;
        
        // ========== 旋转实时跟随 ==========
        transform.rotation = bodyRot * localRotOffset;
    }

    private float GetBodyYaw(Quaternion rotation)
    {
        Vector3 forward = rotation * Vector3.forward;
        float verticality = Mathf.Abs(forward.y);
        
        // 正常站立：使用 forward
        if (verticality < 0.866f)
        {
            forward.y = 0;
            forward.Normalize();
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }
        
        // 低头/抬头：使用 up 向量
        Vector3 up = rotation * Vector3.up;
        up.y = 0;
        
        if (up.sqrMagnitude > 0.001f)
        {
            up.Normalize();
            float yawFromUp = Mathf.Atan2(up.x, up.z) * Mathf.Rad2Deg;
            return (forward.y > 0) ? yawFromUp + 180f : yawFromUp;
        }
        
        return bodyYRot;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmo) return;
        
        // 绘制当前物体
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.2f);
        
        // 绘制检测到的身体朝向（红色）
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Vector3 bodyForward = Quaternion.Euler(0, bodyYRot, 0) * Vector3.forward;
            Gizmos.DrawRay(camT.position, bodyForward * 0.3f);
        }
    }
}