using UnityEngine;
using Unity.XR.PXR;

public class PICO_HapticController : MonoBehaviour
{
    public static PICO_HapticController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TriggerHaptic(ControllerHand hand, float intensity, float duration)
    {
        // int intensity = Mathf.RoundToInt(Mathf.Clamp01(amplitude) * 100);
        int timeMs = Mathf.Max(1, Mathf.RoundToInt(duration * 1000));

        switch (hand)
        {
            case ControllerHand.Left:
                PXR_Input.SetControllerVibration(intensity, timeMs, PXR_Input.Controller.LeftController);
                break;
            case ControllerHand.Right:
                PXR_Input.SetControllerVibration(intensity, timeMs, PXR_Input.Controller.RightController);
                break;
            case ControllerHand.Both:
                PXR_Input.SetControllerVibration(intensity, timeMs, PXR_Input.Controller.LeftController);
                PXR_Input.SetControllerVibration(intensity, timeMs, PXR_Input.Controller.RightController);
                break;
        }
    }

    public void StopHaptic(ControllerHand hand)
    {
        switch (hand)
        {
            case ControllerHand.Left:
                PXR_Input.SetControllerVibration(0, 0, PXR_Input.Controller.LeftController);
                break;
            case ControllerHand.Right:
                PXR_Input.SetControllerVibration(0, 0, PXR_Input.Controller.RightController);
                break;
            case ControllerHand.Both:
                PXR_Input.SetControllerVibration(0, 0, PXR_Input.Controller.LeftController);
                PXR_Input.SetControllerVibration(0, 0, PXR_Input.Controller.RightController);
                break;
        }
    }
}

public enum ControllerHand
{
    Left,
    Right,
    Both
}