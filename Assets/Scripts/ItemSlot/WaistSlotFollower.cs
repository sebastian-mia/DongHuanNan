using UnityEngine;

[AddComponentMenu("XR/WaistSlotFollower")]
public class WaistSlotFollower : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform cameraTransform;

    [Header("平滑设置")]
    [Range(1f, 50f)] public float smoothSpeed = 15f;

    private Vector3 localOffset;
    private Quaternion localRotOffset;
    private float smoothedYaw;

    void Awake()
    {
        if (cameraTransform == null)
        {
            var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            cameraTransform = origin ? origin.Camera.transform : Camera.main?.transform;
        }

        // 初始身体朝向（基于 Camera）
        float initialYaw = GetYaw(cameraTransform.rotation);
        Quaternion bodyRot = Quaternion.Euler(0, initialYaw, 0);
        Quaternion invBody = Quaternion.Inverse(bodyRot);

        // 计算相对偏移
        localOffset = invBody * (transform.position - cameraTransform.position);
        localRotOffset = invBody * transform.rotation;

        smoothedYaw = initialYaw;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // 当前 Camera 朝向
        float targetYaw = GetYaw(cameraTransform.rotation);
        
        // 平滑旋转
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        smoothedYaw = Mathf.LerpAngle(smoothedYaw, targetYaw, t);
        Quaternion bodyRot = Quaternion.Euler(0, smoothedYaw, 0);

        // 位置：Camera位置 + 旋转后的偏移
        transform.position = cameraTransform.position + bodyRot * localOffset;
        
        // 旋转：身体旋转 * 初始相对旋转
        transform.rotation = bodyRot * localRotOffset;
    }

    float GetYaw(Quaternion rotation)
    {
        Vector3 forward = rotation * Vector3.forward;
        
        // 正常情况
        if (Mathf.Abs(forward.y) < 0.866f)
        {
            forward.y = 0;
            forward.Normalize();
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }
        
        // 垂直情况
        Vector3 up = rotation * Vector3.up;
        up.y = 0;
        up.Normalize();
        float yaw = Mathf.Atan2(up.x, up.z) * Mathf.Rad2Deg;
        return forward.y > 0 ? yaw + 180f : yaw;
    }
}