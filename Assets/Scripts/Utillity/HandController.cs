using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using InputDevice = UnityEngine.XR.InputDevice;

public class HandController : MonoBehaviour
{
    [Header("Input Settings")]
    public InputDeviceCharacteristics inputDeviceCharacteristics;
    
    [Header("Attach Transforms")]
    public Transform pokeAttachTransform;
    public Transform directAttachTransform;

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

    // 事件定义
    public static event Action<HandController, float> OnTriggerChanged;
    public static event Action<HandController, float> OnGripChanged;
    public static event Action<HandController, Transform> OnAttachTransformChanged;

    void Awake()
    {
        directInteractor = GetComponentInChildren<XRDirectInteractor>();
        handAnimator = GetComponent<Animator>();
        
        // 初始化输入动作
        InitializeInputActions();
    }

    void OnEnable()
    {
        // 启用输入动作并订阅事件
        triggerAction?.Enable();
        gripAction?.Enable();
        
        // 订阅设备连接事件
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
        
        // 尝试初始化设备
        TryInitializeDevice();
    }

    void OnDisable()
    {
        // 禁用输入动作
        triggerAction?.Disable();
        gripAction?.Disable();
        
        // 取消订阅设备事件
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
        
        // 取消订阅输入事件
        if (triggerAction != null)
            triggerAction.performed -= OnTriggerPerformed;
        if (triggerAction != null)
            triggerAction.canceled -= OnTriggerCanceled;
            
        if (gripAction != null)
            gripAction.performed -= OnGripPerformed;
        if (gripAction != null)
            gripAction.canceled -= OnGripCanceled;
    }

    // 初始化输入动作系统
    private void InitializeInputActions()
    {
        // 根据设备特征确定左右手
        bool isLeftHand = (inputDeviceCharacteristics & InputDeviceCharacteristics.Left) != 0;
        string handName = isLeftHand ? "LeftHand" : "RightHand";
        
        // 创建触发器动作
        triggerAction = new InputAction(
            name: $"{handName}Trigger",
            type: InputActionType.Value,
            binding: $"<XRController>{{{handName}}}/trigger"
        );
        
        // 创建握把动作
        gripAction = new InputAction(
            name: $"{handName}Grip",
            type: InputActionType.Value,
            binding: $"<XRController>{{{handName}}}/grip"
        );
        
        // 订阅输入事件
        triggerAction.performed += OnTriggerPerformed;
        triggerAction.canceled += OnTriggerCanceled;
        
        gripAction.performed += OnGripPerformed;
        gripAction.canceled += OnGripCanceled;
    }

    // 尝试初始化设备
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

    // 设备连接事件
    private void OnDeviceConnected(InputDevice device)
    {
        if ((device.characteristics & inputDeviceCharacteristics) == inputDeviceCharacteristics)
        {
            targetDevice = device;
            Debug.Log($"[HandController] 设备已连接: {device.name}");
        }
    }

    // 设备断开事件
    private void OnDeviceDisconnected(InputDevice device)
    {
        if (device == targetDevice)
        {
            targetDevice = default;
            Debug.Log($"[HandController] 设备已断开");
            
            // 重置状态
            UpdateTriggerValue(0f);
            UpdateGripValue(0f);
        }
    }

    // 触发器按下/变化事件
    private void OnTriggerPerformed(InputAction.CallbackContext context)
    {
        float value = context.ReadValue<float>();
        UpdateTriggerValue(value);
    }

    // 触发器释放事件
    private void OnTriggerCanceled(InputAction.CallbackContext context)
    {
        UpdateTriggerValue(0f);
    }

    // 握把按下/变化事件
    private void OnGripPerformed(InputAction.CallbackContext context)
    {
        float value = context.ReadValue<float>();
        UpdateGripValue(value);
    }

    // 握把释放事件
    private void OnGripCanceled(InputAction.CallbackContext context)
    {
        UpdateGripValue(0f);
    }

    // 更新触发器值
    private void UpdateTriggerValue(float value)
    {
        if (Mathf.Approximately(currentTriggerValue, value)) return;
        
        currentTriggerValue = value;
        
        // 更新动画
        handAnimator?.SetFloat("Trigger", value);
        
        // 触发全局事件
        OnTriggerChanged?.Invoke(this, value);
        
        // 更新AttachTransform
        UpdateAttachTransform();
    }

    // 更新握把值
    private void UpdateGripValue(float value)
    {
        if (Mathf.Approximately(currentGripValue, value)) return;
        
        currentGripValue = value;
        
        // 更新动画
        handAnimator?.SetFloat("Grab", value);
        
        // 触发全局事件
        OnGripChanged?.Invoke(this, value);
        
        // 更新AttachTransform
        UpdateAttachTransform();
    }

    // 更新AttachTransform（基于当前状态）
    private void UpdateAttachTransform()
    {
        if (directInteractor == null) return;
        
        Transform newAttachTransform = null;
        
        // 优先级逻辑：都按下时使用direct，否则根据单独按键，都没按下时为null
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
        
        // 只有变化时才设置
        if (directInteractor.attachTransform != newAttachTransform)
        {
            directInteractor.attachTransform = newAttachTransform;
            OnAttachTransformChanged?.Invoke(this, newAttachTransform);
        }
    }

    // 公共属性供外部访问
    public float TriggerValue => currentTriggerValue;
    public float GripValue => currentGripValue;
    public bool IsTriggerPressed => currentTriggerValue >= 0.9f;
    public bool IsGripPressed => currentGripValue >= 0.9f;
    public InputDeviceCharacteristics DeviceCharacteristics => inputDeviceCharacteristics;
}