using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRLocomotionPause : MonoBehaviour
{
    public static XRLocomotionPause Instance { get; private set; }

    [SerializeField] private LocomotionSystem loco;   // 2.5.3 自带
    private bool[] cachedState;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    public void Pause()
    {
        var providers = loco.GetComponents<LocomotionProvider>();
        cachedState = new bool[providers.Length];
        for (int i = 0; i < providers.Length; i++)
        {
            cachedState[i] = providers[i].enabled;
            providers[i].enabled = false;
        }
    }

    public void Resume()
    {
        var providers = loco.GetComponents<LocomotionProvider>();
        for (int i = 0; i < providers.Length; i++)
            providers[i].enabled = cachedState[i];
    }
}