// 脚本2: PICO_HapticOnInteract.cs
// 挂载位置: 可交互物体（如道具、按钮）
// 作用: 交互时自动触发震动

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRBaseInteractable))]
public class PICO_HapticOnInteract : MonoBehaviour
{
    [SerializeField] private float hoverAmp = 0.2f;
    [SerializeField] private float selectAmp = 0.2f;
    [SerializeField]
    private InputActionReference leftTriggerAction;
    [SerializeField]
    private InputActionReference rightTriggerAction;

    private XRBaseInteractable _interactable;

    void Awake()
    {
        _interactable = GetComponent<XRBaseInteractable>();

        _interactable.hoverEntered.AddListener(OnHover);
        _interactable.selectEntered.AddListener(OnSelect);
    }

    void OnHover(HoverEnterEventArgs args)
    {
        if (!leftTriggerAction.action.triggered && !rightTriggerAction.action.triggered)
        {
            var hand = GetHand(args.interactor);
            PICO_HapticController.Instance?.TriggerHaptic(hand, hoverAmp, 0.05f);
        }
    }

    void OnSelect(SelectEnterEventArgs args)
    {
        var hand = GetHand(args.interactor);
        PICO_HapticController.Instance?.TriggerHaptic(hand, selectAmp, 0.1f);
    }

    ControllerHand GetHand(XRBaseInteractor interactor)
    {
        // 通过 PICO 控制器判断
        if (interactor.name.Contains("Left")) return ControllerHand.Left;
        return ControllerHand.Right;
    }
}