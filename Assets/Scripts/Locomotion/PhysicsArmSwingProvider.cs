using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PhysicsArmSwingProvider : LocomotionProvider
{
    [Header("Controller References")]
    public Transform leftController;
    public Transform rightController;
    public Transform headTransform;

    [Header("Input Control")]
    [Tooltip("是否需要按住Trigger键才能摆臂移动")]
    public bool requireTriggerPressed = false;
    [Tooltip("左手Trigger Action")]
    public InputActionReference leftTriggerAction;
    [Tooltip("右手Trigger Action")]
    public InputActionReference rightTriggerAction;
    [Tooltip("Trigger按下的阈值")]
    public float triggerThreshold = 0.1f;

    [Header("Movement Settings")]
    public float baseSpeed = 2.5f;
    public float minSwingSpeed = 0.3f;
    public float maxSwingSpeed = 4.0f;
    public float moveSmoothing = 0.15f;
    public float stopSmoothing = 0.3f;
    public float momentumDuration = 0.4f;

    [Header("Detection")]
    public int historySize = 5;

    // 速度历史
    private List<float> swingIntensityHistory = new List<float>();
    private Vector3 lastLeftPos;
    private Vector3 lastRightPos;
    private bool initialized = false;
    
    private float currentSpeed = 0f;
    private float momentumTimer = 0f;
    private CharacterController characterController;
    private bool hasExclusiveLock = false;

    // Trigger状态
    private bool leftTriggerPressed = false;
    private bool rightTriggerPressed = false;

    // 用于Vignette检测 - 关键：跟踪移动状态
    private bool m_IsMoving = false;

    protected override void Awake()
    {
        base.Awake();
    }

    void OnEnable()
    {
        leftTriggerAction?.action?.Enable();
        rightTriggerAction?.action?.Enable();
    }

    void Start()
    {
        FindReferences();
        
        if (system?.xrOrigin != null)
        {
            characterController = system.xrOrigin.GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = system.xrOrigin.gameObject.AddComponent<CharacterController>();
                characterController.center = new Vector3(0, 1.0f, 0);
                characterController.radius = 0.3f;
                characterController.height = 1.6f;
            }
        }

        StartCoroutine(DelayedInit());
    }

    System.Collections.IEnumerator DelayedInit()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (leftController != null) lastLeftPos = leftController.position;
        if (rightController != null) lastRightPos = rightController.position;
        
        initialized = true;
    }

    void FindReferences()
    {
        if (system?.xrOrigin == null) return;

        if (headTransform == null)
            headTransform = system.xrOrigin.Camera.transform;

        if (leftController == null || rightController == null)
        {
            var controllers = system.xrOrigin.GetComponentsInChildren<XRBaseController>(true);
            foreach (var ctrl in controllers)
            {
                var t = ctrl.transform;
                if (leftController == null && t.name.Contains("Left"))
                    leftController = t;
                else if (rightController == null && t.name.Contains("Right"))
                    rightController = t;
            }
        }
    }

    void Update()
    {
        // 关键检查1：组件是否启用
        if (!enabled) 
        {
            TryReleaseLock();
            UpdateLocomotionPhase();
            return;
        }
        
        // 关键检查2：LocomotionSystem 是否存在且启用
        if (system == null || !system.enabled)
        {
            TryReleaseLock();
            UpdateLocomotionPhase();
            return;
        }

        if (!initialized || leftController == null || rightController == null)
        {
            UpdateLocomotionPhase();
            return;
        }

        // 更新Trigger输入
        UpdateTriggerInput();

        // 计算两手速度（只关心大小，不关心方向）
        Vector3 leftVel = (leftController.position - lastLeftPos) / Time.deltaTime;
        Vector3 rightVel = (rightController.position - lastRightPos) / Time.deltaTime;

        // 忽略Y轴，只算水平速度大小
        leftVel.y = 0;
        rightVel.y = 0;
        float leftSpeed = leftVel.magnitude;
        float rightSpeed = rightVel.magnitude;

        // 取平均摆臂强度
        float swingIntensity = (leftSpeed + rightSpeed) * 0.5f;

        // 更新历史
        swingIntensityHistory.Add(swingIntensity);
        if (swingIntensityHistory.Count > historySize)
            swingIntensityHistory.RemoveAt(0);

        // 获取平滑后的强度
        float avgIntensity = GetAverage(swingIntensityHistory);

        // 检查Trigger条件
        bool triggerCondition = !requireTriggerPressed || (leftTriggerPressed && rightTriggerPressed);

        // 计算目标速度
        float targetSpeed = 0f;
        if (triggerCondition && avgIntensity > minSwingSpeed)
        {
            float normalized = Mathf.Clamp01((avgIntensity - minSwingSpeed) / (maxSwingSpeed - minSwingSpeed));
            targetSpeed = normalized * baseSpeed;
        }

        // 平滑处理速度
        UpdateSpeed(targetSpeed);

        // 关键：只要currentSpeed > 0就申请锁并移动
        m_IsMoving = false;
        if (currentSpeed > 0.01f)
        {
            // 申请或保持移动权限
            if (!hasExclusiveLock)
            {
                if (BeginLocomotion())
                {
                    hasExclusiveLock = true;
                }
            }

            // 有权限就移动
            if (hasExclusiveLock)
            {
                MoveForward();
                m_IsMoving = true;
            }
        }
        else
        {
            // 速度为0，释放权限
            TryReleaseLock();
        }

        // 更新locomotionPhase - 关键！这会被TunnelingVignetteController检测
        UpdateLocomotionPhase();

        // 更新记录
        lastLeftPos = leftController.position;
        lastRightPos = rightController.position;
    }

    void UpdateLocomotionPhase()
    {
        // 参考 ContinuousMoveProviderBase 的实现
        switch (locomotionPhase)
        {
            case LocomotionPhase.Idle:
            case LocomotionPhase.Started:
                if (m_IsMoving)
                    locomotionPhase = LocomotionPhase.Moving;
                break;
            case LocomotionPhase.Moving:
                if (!m_IsMoving)
                    locomotionPhase = LocomotionPhase.Done;
                break;
            case LocomotionPhase.Done:
                locomotionPhase = m_IsMoving ? LocomotionPhase.Moving : LocomotionPhase.Idle;
                break;
        }
    }

    void UpdateTriggerInput()
    {
        leftTriggerPressed = false;
        rightTriggerPressed = false;

        if (!requireTriggerPressed)
            return;

        // 读取左手Trigger
        if (leftTriggerAction != null && leftTriggerAction.action != null)
        {
            float value = leftTriggerAction.action.ReadValue<float>();
            leftTriggerPressed = value > triggerThreshold;
        }

        // 读取右手Trigger
        if (rightTriggerAction != null && rightTriggerAction.action != null)
        {
            float value = rightTriggerAction.action.ReadValue<float>();
            rightTriggerPressed = value > triggerThreshold;
        }
    }

    void UpdateSpeed(float targetSpeed)
    {
        if (targetSpeed > 0.01f)
        {
            // 加速
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, moveSmoothing);
            momentumTimer = 0f;
        }
        else
        {
            // 减速（动量衰减）
            momentumTimer += Time.deltaTime;
            currentSpeed = Mathf.Lerp(currentSpeed, 0f, stopSmoothing);
            
            if (currentSpeed < 0.01f)
                currentSpeed = 0f;
        }
    }

    void MoveForward()
    {
        if (characterController == null) return;

        Vector3 moveDir = headTransform.forward;
        moveDir.y = 0;
        moveDir.Normalize();
        
        Vector3 velocity = moveDir * currentSpeed;
        
        // 应用重力
        velocity.y = characterController.isGrounded ? -0.5f : characterController.velocity.y;
        
        characterController.Move(velocity * Time.deltaTime);
    }

    void TryReleaseLock()
    {
        if (hasExclusiveLock)
        {
            EndLocomotion();
            hasExclusiveLock = false;
        }
        currentSpeed = 0f;
        momentumTimer = 0f;
        m_IsMoving = false;
    }

    float GetAverage(List<float> history)
    {
        if (history.Count == 0) return 0f;
        float sum = 0f;
        foreach (var v in history) sum += v;
        return sum / history.Count;
    }

    void OnDisable()
    {
        TryReleaseLock();
        UpdateLocomotionPhase();
        leftTriggerAction?.action?.Disable();
        rightTriggerAction?.action?.Disable();
    }

    // void OnGUI()
    // {
    //     if (!initialized) return;
    //     
    //     GUILayout.BeginArea(new Rect(10, 10, 250, 180));
    //     GUILayout.Label($"=== Arm Swing ===");
    //     GUILayout.Label($"Require Trigger: {requireTriggerPressed}");
    //     if (requireTriggerPressed)
    //     {
    //         GUILayout.Label($"L Trigger: {leftTriggerPressed}");
    //         GUILayout.Label($"R Trigger: {rightTriggerPressed}");
    //     }
    //     GUILayout.Label($"Current Speed: {currentSpeed:F2}");
    //     GUILayout.Label($"Is Moving: {m_IsMoving}");
    //     GUILayout.Label($"Phase: {locomotionPhase}");
    //     GUILayout.EndArea();
    // }
}