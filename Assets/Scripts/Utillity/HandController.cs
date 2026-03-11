using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using InputDevice = UnityEngine.XR.InputDevice;

public class HandController : MonoBehaviour
{
    [Header("Input Settings")]
    public InputDeviceCharacteristics inputDeviceCharacteristics;
    
    [Header("Attach Transforms")]
    public Transform pokeAttachTransform;
    public Transform directAttachTransform;

    [Header("Animation Settings")]
    [Tooltip("0 = 无延迟即时响应, 0.1 = 轻微平滑")]
    [Range(0f, 0.2f)]
    public float animationResponseSpeed = 0f;

    // 组件引用
    private Animator handAnimator;
    private XRDirectInteractor directInteractor;
    private InputDevice targetDevice;
    
    // 输入动作
    private InputAction triggerAction;
    private InputAction gripAction;
    
    // 状态值
    private float currentTriggerValue = 0f;
    private float currentGripValue = 0f;
    private float targetTriggerValue = 0f;
    private float targetGripValue = 0f;
    private bool isSelecting = false;

    // 缓存默认 attachTransform
    private Transform defaultAttachTransform;

    // 静态字典存储实例
    private static Dictionary<InputDeviceCharacteristics, HandController> instances = new Dictionary<InputDeviceCharacteristics, HandController>();

    // 事件定义
    public static event Action<HandController, float> OnTriggerChanged;
    public static event Action<HandController, float> OnGripChanged;
    public static event Action<HandController, Transform> OnAttachTransformChanged;

    void Awake()
    {
        // 单例检查
        if (instances.TryGetValue(inputDeviceCharacteristics, out HandController existing))
        {
            if (existing != null && existing != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        
        instances[inputDeviceCharacteristics] = this;
        
        directInteractor = GetComponentInChildren<XRDirectInteractor>();
        handAnimator = GetComponentInChildren<Animator>();
        
        // // 缓存默认 attachTransform
        // if (directInteractor != null)
        // {
        //     defaultAttachTransform = directInteractor.attachTransform;
        // }
        
        // 监听抓取事件
        // if (directInteractor != null)
        // {
        //     directInteractor.selectEntered.AddListener(OnSelectEntered);
        //     directInteractor.selectExited.AddListener(OnSelectExited);
        // }
        
        InitializeInputActions();
    }

    void Update()
    {
        // 每帧直接读取输入 - 最低延迟
        ReadInputImmediate();
        
        // 更新动画
        UpdateAnimationImmediate();
    }

    void OnEnable()
    {
        triggerAction?.Enable();
        gripAction?.Enable();
        
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
        
        TryInitializeDevice();
        
        // 恢复时强制刷新动画参数，防止系统隐藏/显示后动画重置
        if (handAnimator != null)
        {
            handAnimator.SetFloat("Trigger", currentTriggerValue);
            handAnimator.SetFloat("Grab", currentGripValue);
            handAnimator.Update(0);
        }
    }

    void OnDisable()
    {
        triggerAction?.Disable();
        gripAction?.Disable();
        
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }

    void OnDestroy()
    {
        if (instances.TryGetValue(inputDeviceCharacteristics, out HandController current) && current == this)
        {
            instances.Remove(inputDeviceCharacteristics);
        }
        
        // if (directInteractor != null)
        // {
        //     directInteractor.selectEntered.RemoveListener(OnSelectEntered);
        //     directInteractor.selectExited.RemoveListener(OnSelectExited);
        // }
    }

    // 立即读取输入，无回调延迟
    private void ReadInputImmediate()
    {
        if (triggerAction != null && triggerAction.enabled)
        {
            targetTriggerValue = triggerAction.ReadValue<float>();
        }
        
        if (gripAction != null && gripAction.enabled)
        {
            targetGripValue = gripAction.ReadValue<float>();
        }
    }

    // 即时动画更新
    private void UpdateAnimationImmediate()
    {
        // 检测变化
        bool triggerChanged = !Mathf.Approximately(currentTriggerValue, targetTriggerValue);
        bool gripChanged = !Mathf.Approximately(currentGripValue, targetGripValue);
        
        if (!triggerChanged && !gripChanged) return;
        
        // 即时或快速插值到目标值
        if (animationResponseSpeed <= 0f)
        {
            // 完全即时 - 零延迟
            if (triggerChanged)
            {
                currentTriggerValue = targetTriggerValue;
                handAnimator?.SetFloat("Trigger", currentTriggerValue);
                OnTriggerChanged?.Invoke(this, currentTriggerValue);
            }
            
            if (gripChanged)
            {
                currentGripValue = targetGripValue;
                handAnimator?.SetFloat("Grab", currentGripValue);
                OnGripChanged?.Invoke(this, currentGripValue);
            }
        }
        else
        {
            // 极快插值 - 视觉平滑但响应迅速
            float delta = Time.deltaTime / animationResponseSpeed;
            
            if (triggerChanged)
            {
                currentTriggerValue = Mathf.MoveTowards(currentTriggerValue, targetTriggerValue, delta);
                handAnimator?.SetFloat("Trigger", currentTriggerValue);
                
                if (Mathf.Approximately(currentTriggerValue, targetTriggerValue))
                    OnTriggerChanged?.Invoke(this, currentTriggerValue);
            }
            
            if (gripChanged)
            {
                currentGripValue = Mathf.MoveTowards(currentGripValue, targetGripValue, delta);
                handAnimator?.SetFloat("Grab", currentGripValue);
                
                if (Mathf.Approximately(currentGripValue, targetGripValue))
                    OnGripChanged?.Invoke(this, currentGripValue);
            }
        }
        
        // 只在非抓取状态下更新 attachTransform
        if (!isSelecting && (triggerChanged || gripChanged))
        {
            UpdateAttachTransform();
        }
    }

    // private void OnSelectEntered(SelectEnterEventArgs args)
    // {
    //     isSelecting = true;
    //     
    //     // 抓取前重置 attachTransform 为默认值，防止变形
    //     if (directInteractor != null && defaultAttachTransform != null)
    //     {
    //         directInteractor.attachTransform = defaultAttachTransform;
    //     }
    //     
    //     // 注意：隐藏由系统控制（XRDirectInteractor.hideControllerOnSelect）
    // }

    // private void OnSelectExited(SelectExitEventArgs args)
    // {
    //     isSelecting = false;
    //     
    //     // 恢复时重新评估 attachTransform
    //     UpdateAttachTransform();
    //     
    //     // 注意：显示由系统控制（XRDirectInteractor.hideControllerOnSelect）
    // }

    private void UpdateAttachTransform()
    {
        if (directInteractor == null || isSelecting) return;
        
        Transform newAttachTransform = null;
        
        if (currentTriggerValue >= 0.9f && currentGripValue >= 0.9f)
        {
            newAttachTransform = directAttachTransform;
        }
        else if (currentGripValue >= 0.9f)
        {
            newAttachTransform = directAttachTransform;
        }
        else if (currentTriggerValue >= 0.9f)
        {
            newAttachTransform = pokeAttachTransform;
        }
        else
        {
            // 无输入时恢复默认
            newAttachTransform = defaultAttachTransform;
        }
        
        if (directInteractor.attachTransform != newAttachTransform)
        {
            directInteractor.attachTransform = newAttachTransform;
            OnAttachTransformChanged?.Invoke(this, newAttachTransform);
        }
    }

    private void InitializeInputActions()
    {
        bool isLeftHand = (inputDeviceCharacteristics & InputDeviceCharacteristics.Left) != 0;
        string handName = isLeftHand ? "LeftHand" : "RightHand";
        
        // 使用 PassThrough 类型获得最低延迟
        triggerAction = new InputAction(
            name: $"{handName}Trigger",
            type: InputActionType.PassThrough,
            binding: $"<XRController>{{{handName}}}/trigger"
        );
        
        gripAction = new InputAction(
            name: $"{handName}Grip",
            type: InputActionType.PassThrough,
            binding: $"<XRController>{{{handName}}}/grip"
        );
        
        // 可选：设置更新模式为动态更新
        triggerAction.actionMap?.Enable();
    }

    private void TryInitializeDevice()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(inputDeviceCharacteristics, devices);
        
        if (devices.Count > 0 && !targetDevice.isValid)
        {
            targetDevice = devices[0];
            Debug.Log($"[HandController] 设备初始化成功: {targetDevice.name}");
        }
    }

    private void OnDeviceConnected(InputDevice device)
    {
        if ((device.characteristics & inputDeviceCharacteristics) == inputDeviceCharacteristics)
        {
            targetDevice = device;
            Debug.Log($"[HandController] 设备已连接: {device.name}");
        }
    }

    private void OnDeviceDisconnected(InputDevice device)
    {
        if (device == targetDevice)
        {
            targetDevice = default;
            targetTriggerValue = 0f;
            targetGripValue = 0f;
            Debug.Log($"[HandController] 设备已断开");
        }
    }

    // 公共属性
    public float TriggerValue => currentTriggerValue;
    public float GripValue => currentGripValue;
    public bool IsTriggerPressed => currentTriggerValue >= 0.9f;
    public bool IsGripPressed => currentGripValue >= 0.9f;
    public bool IsSelecting => isSelecting;
    public InputDeviceCharacteristics DeviceCharacteristics => inputDeviceCharacteristics;
}