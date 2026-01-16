using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Content.Interaction;

[RequireComponent(typeof(XRKnob))]
public class XRWellHandle : MonoBehaviour
{
    #region === 字段 ===
    private XRKnob knob;
    private IXRSelectInteractor interactor;
    private bool isGrabbed = false;

    // 旋转计算
    private float grabControllerLocalEulerY;  // 抓取瞬间控制器本地欧拉角（连续）
    private float previousHandleContinuousAngle; // 上一次的连续角度
    private float handleContinuousAngle;      // 手柄的连续角度（永远不跳变）

    // 极限处理
    private float limitDeadzone = 0.5f;
    private bool isAtLimit = false;

    [Header("灵敏度设置")]
    [SerializeField, Range(0.5f, 2f)]
    private float sensitivityMultiplier = 1f;

    [Header("调试设置")]
    [SerializeField] private bool showDebugInfo = true;
    private string debugInfo = "";
    #endregion

    #region === 生命周期 ===
    void Awake()
    {
        knob = GetComponent<XRKnob>();

        // 给连续角度赋初值
        handleContinuousAngle = knob.handle ? knob.handle.localEulerAngles.y : 0f;
        previousHandleContinuousAngle = handleContinuousAngle;
    }

    void Start() => UpdateKnobValueFromAngle(handleContinuousAngle);

    void OnEnable()
    {
        knob.selectEntered.AddListener(OnGrab);
        knob.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        knob.selectEntered.RemoveListener(OnGrab);
        knob.selectExited.RemoveListener(OnRelease);
    }
    #endregion

    #region === 抓放事件 ===
    void OnGrab(SelectEnterEventArgs args)
    {
        interactor = args.interactorObject;
        isGrabbed = true;
        isAtLimit = false;
        grabControllerLocalEulerY = GetControllerLocalEulerY();
        previousHandleContinuousAngle = handleContinuousAngle;

        // debugInfo = $"抓取开始: continuousAngle={handleContinuousAngle:F1}, 灵敏度={sensitivityMultiplier:F2}";
        // if (showDebugInfo) Debug.Log(debugInfo);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (knob.clampedMotion)
        {
            // 限制角度范围
            handleContinuousAngle = Mathf.Clamp(handleContinuousAngle, knob.minAngle, knob.maxAngle);
        }

        // 更新视觉和数值
        UpdateHandleVisual(handleContinuousAngle);
        UpdateKnobValueFromAngle(handleContinuousAngle);

        isGrabbed = false;
        interactor = null;
        isAtLimit = false;

        // debugInfo = $"抓取结束: angle={handleContinuousAngle:F1}, value={knob.value:F3}";
        // if (showDebugInfo) Debug.Log(debugInfo);
    }
    #endregion

    #region === 每帧旋转逻辑 ===
    void Update()
    {
        if (!isGrabbed) return;

        float currentControllerLocalEulerY = GetControllerLocalEulerY();
        float rawAngleDelta = Mathf.DeltaAngle(grabControllerLocalEulerY, currentControllerLocalEulerY);
        
        // 应用灵敏度调整
        float angleDelta = rawAngleDelta * sensitivityMultiplier;

        // 忽略微小变化
        if (Mathf.Abs(angleDelta) < 0.05f) 
        {
            angleDelta = 0f;
        }

        // 检查是否在极限区域
        bool atMin = false;
        bool atMax = false;
        
        if (knob.clampedMotion)
        {
            atMin = handleContinuousAngle <= knob.minAngle + limitDeadzone;
            atMax = handleContinuousAngle >= knob.maxAngle - limitDeadzone;
            isAtLimit = atMin || atMax;
        }
        else
        {
            isAtLimit = false;
        }

        // 极限区域处理
        if (isAtLimit)
        {
            // 如果试图越过极限，阻止移动并重新同步控制器角度
            if ((atMin && angleDelta < 0) || (atMax && angleDelta > 0))
            {
                angleDelta = 0f;
                // 同步控制器角度，避免累积误差
                grabControllerLocalEulerY = currentControllerLocalEulerY;
                
                // if (showDebugInfo && Mathf.Abs(rawAngleDelta) > 10f)
                    // Debug.Log($"空转: 控制器旋转{rawAngleDelta:F1}°，但手柄不动，已同步基准角度");
            }
            else if ((atMin && angleDelta > 0) || (atMax && angleDelta < 0))
            {
                // 从极限区域移出，给予轻微加速效果
                grabControllerLocalEulerY = currentControllerLocalEulerY;
                angleDelta *= 1.1f; // 轻微加速，提高手感
            }
        }
        else
        {
            // 正常情况：更新控制器基准角度
            grabControllerLocalEulerY = currentControllerLocalEulerY;
        }

        // 应用角度变化
        if (Mathf.Abs(angleDelta) > 0.001f)
        {
            float newAngle = handleContinuousAngle + angleDelta;
            
            // 对于非限制模式，保持角度在合理范围（0-360）
            if (!knob.clampedMotion)
            {
                newAngle = Mathf.Repeat(newAngle, 360f);
            }
            else if (knob.clampedMotion)
            {
                newAngle = Mathf.Clamp(newAngle, knob.minAngle, knob.maxAngle);
            }
            
            // 检查角度是否真正变化
            if (Mathf.Abs(Mathf.DeltaAngle(handleContinuousAngle, newAngle)) > 0.01f)
            {
                handleContinuousAngle = newAngle;
                
                // 更新视觉和数值
                UpdateHandleVisual(handleContinuousAngle);
                UpdateKnobValueFromAngle(handleContinuousAngle);
                
                previousHandleContinuousAngle = handleContinuousAngle;
            }
        }

        // 调试信息
        // if (showDebugInfo && Mathf.Abs(angleDelta) > 1f)
        // {
        //     debugInfo = $"旋转: angle={handleContinuousAngle:F1}, value={knob.value:F3}, delta={angleDelta:F1}";
        //     if (isAtLimit) debugInfo += " [LIMIT]";
        //     Debug.Log(debugInfo);
        // }
    }
    #endregion

    #region === 工具函数 ===
    // 返回控制器在手柄本地空间的连续欧拉角（Y 轴）
    float GetControllerLocalEulerY()
    {
        if (interactor == null || knob.handle == null || knob.handle.parent == null) 
            return 0f;

        try
        {
            Transform attachTransform = interactor.GetAttachTransform(knob);
            if (attachTransform == null) return 0f;

            Vector3 controllerWorld = attachTransform.position;
            Vector3 localPos = knob.handle.parent.InverseTransformPoint(controllerWorld);
            Vector3 handleLocalPos = knob.handle.localPosition;
            Vector3 localDir = localPos - handleLocalPos;
            
            // 确保方向向量有足够长度
            if (localDir.sqrMagnitude < 0.0001f) return 0f;
            
            // 忽略垂直分量，只考虑XZ平面
            float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            
            // 转换为0-360范围
            return (angle < 0) ? angle + 360f : angle;
        }
        catch
        {
            return 0f;
        }
    }

    // 更新手柄视觉旋转
    void UpdateHandleVisual(float targetAngle)
    {
        if (!knob.handle) return;
        
        // 使用Quaternion进行插值，避免万向锁问题
        Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
        knob.handle.localRotation = targetRotation;
    }

    // 从角度更新旋钮值
    void UpdateKnobValueFromAngle(float angle)
    {
        if (knob == null) return;

        float newValue;
        
        if (knob.clampedMotion)
        {
            // 对于限制模式，角度已在限制范围内
            float clampedAngle = Mathf.Clamp(angle, knob.minAngle, knob.maxAngle);
            newValue = Mathf.InverseLerp(knob.minAngle, knob.maxAngle, clampedAngle);
            
            // 如果角度被限制，同步连续角度
            if (Mathf.Abs(angle - clampedAngle) > 0.01f)
            {
                handleContinuousAngle = clampedAngle;
                UpdateHandleVisual(clampedAngle);
            }
        }
        else
        {
            // 对于非限制模式，将角度映射到0-1范围
            float range = knob.maxAngle - knob.minAngle;
            if (Mathf.Abs(range) < 0.001f)
            {
                // 默认360度范围
                range = 360f;
                newValue = Mathf.Repeat(angle - knob.minAngle, 360f) / 360f;
            }
            else
            {
                newValue = Mathf.Repeat(angle - knob.minAngle, 360f) / range;
                newValue = Mathf.Clamp01(newValue);
            }
        }
        
        knob.value = newValue;
    }

    // 公共方法
    public float GetAccumulatedRotation() => handleContinuousAngle;

    public void SetSensitivityMultiplier(float multiplier)
    {
        sensitivityMultiplier = Mathf.Clamp(multiplier, 0.5f, 2f);
    }

    public void ForceResync()
    {
        if (isGrabbed)
        {
            grabControllerLocalEulerY = GetControllerLocalEulerY();
            if (showDebugInfo) Debug.Log("强制重新同步控制器角度");
        }
    }
    #endregion

    #region === 编辑器扩展 ===
    void OnValidate()
    {
        if (knob == null) knob = GetComponent<XRKnob>();
        limitDeadzone = Mathf.Max(0.1f, limitDeadzone);
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo || !Application.isPlaying || !isGrabbed || !knob.handle) return;

        Vector3 handlePos = knob.handle.position;

        // 绘制控制器方向
        float controllerAngle = GetControllerLocalEulerY();
        Vector3 controllerDir = Quaternion.Euler(0, controllerAngle, 0) * Vector3.forward;
        controllerDir = transform.TransformDirection(controllerDir);
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(handlePos, controllerDir * 0.15f);

        // 绘制手柄方向
        Vector3 knobDir = Quaternion.Euler(0, handleContinuousAngle, 0) * Vector3.forward;
        knobDir = transform.TransformDirection(knobDir);
        Gizmos.color = isAtLimit ? Color.red : Color.green;
        Gizmos.DrawRay(handlePos, knobDir * 0.12f);

        // 绘制极限方向
        if (knob.clampedMotion)
        {
            Vector3 minDir = Quaternion.Euler(0, knob.minAngle, 0) * Vector3.forward;
            minDir = transform.TransformDirection(minDir);
            Gizmos.color = handleContinuousAngle <= knob.minAngle + limitDeadzone ? Color.red : Color.blue;
            Gizmos.DrawRay(handlePos, minDir * 0.1f);

            Vector3 maxDir = Quaternion.Euler(0, knob.maxAngle, 0) * Vector3.forward;
            maxDir = transform.TransformDirection(maxDir);
            Gizmos.color = handleContinuousAngle >= knob.maxAngle - limitDeadzone ? Color.red : Color.cyan;
            Gizmos.DrawRay(handlePos, maxDir * 0.1f);
        }
    }
    #endregion
}