using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class LocomotionModeController : MonoBehaviour
{
    [SerializeField] private LocomotionSystem locomotionSystem;
    [SerializeField] private TeleportationProvider teleportationProvider;
    [SerializeField] private PhysicsArmSwingProvider physicsArmSwingProvider;
    private readonly string turnTag = "Turn";

    [Header("控制器管理")] [SerializeField] private ActionBasedControllerManager rightControllerManager;

    private void Start()
    {
        SetLocomotionMode(LocomotionMode.挥动);
    }

    public void SetLocomotionMode(LocomotionMode mode)
    {
        DisableAllProviders();

        switch (mode)
        {
            case LocomotionMode.挥动:
                physicsArmSwingProvider.enabled = true;
                // ========== 调用新方法 ==========
                SetTeleportInputEnabled(false);
                break;

            case LocomotionMode.传送:
                teleportationProvider.enabled = true;
                // ========== 调用新方法 ==========
                SetTeleportInputEnabled(true);
                break;
        }
    }

    // ========== 新增：单独的方法控制传送输入 ==========
    public void SetTeleportInputEnabled(bool enabled)
    {
        if (rightControllerManager == null) return;

        // 通过 smoothMotionEnabled 控制传送输入
        // true = 启用平滑移动，禁用传送
        // false = 禁用平滑移动，启用传送
        rightControllerManager.smoothMotionEnabled = !enabled;
    }

    // ========== 你的原始方法，完全不变 ==========
    private void DisableAllProviders()
    {
        for (var i = 0; i < locomotionSystem.transform.childCount; i++)
        {
            var provider = locomotionSystem.transform.GetChild(i);
            if (provider != null)
            {
                if (!provider.gameObject.CompareTag(turnTag))
                {
                    provider.GetComponent<LocomotionProvider>().enabled = false;
                }
            }
        }
    }
}