using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using System.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using System.Collections;
using QFramework;

public class VRSceneManager : MonoBehaviour, IController
{
    public static VRSceneManager Instance { get; private set; }

    [Header("Scene References")] [SerializeField]
    private Transform xrOrigin;

    [SerializeField] private CameraFade cameraFader;
    [SerializeField] private LoadingScreen loadingScreen;

    [Header("XR Locomotion Control")] [SerializeField]
    private LocomotionSystem locomotionSystem;

    [SerializeField] private bool disableMovementDuringLoading = true;

    [Header("场景特定移动限制")] [SerializeField]
    private List<string> restrictMovementScenes = new List<string>() { "MenuScene", "MainMenu" };

    [SerializeField] private bool autoRestrictInMenu = true;

    [Header("Spawn Point Settings")] [SerializeField]
    private string spawnPointTag = "StartPoint";

    [SerializeField] private bool autoMoveToSpawnPoint = true;

    [Header("Fade Settings")] [SerializeField]
    private float fadeInDuration = 3f;

    [SerializeField] private int blackScreenFrames = 2;

    [Header("Controller Settings")] [SerializeField]
    private string leftControllerTag = "LeftController";

    [SerializeField] private string rightControllerTag = "RightController";

    private List<Scene> loadedScenes = new List<Scene>();
    private bool isLoading = false;
    private bool isMovementRestricted = false;

    private GameObject leftController;
    private GameObject rightController;

    private bool ControllersCached => leftController != null || rightController != null;

    private Vector3 leftControllerOriginalScale = Vector3.one;
    private Vector3 rightControllerOriginalScale = Vector3.one;

    public static event Action<Transform> OnXROriginChanged;
    public event Action<string> OnSceneLoaded;
    public event Action<string> OnSceneUnloaded;

    [Header("WaistSlots")] public GameObject turnoutWaistSlots;

    private Vector3 slotsOriginalScale = Vector3.one;
    private bool slotsScaleCached = false;

    private GameRecordModel gameRecordModel;
    private TurnoutDatabaseManager turnoutDatabaseManager;

    // 🔥 异步资源卸载状态
    private AsyncOperation _pendingUnloadOperation;
    private bool _isUnloading = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (locomotionSystem == null)
            locomotionSystem = FindObjectOfType<LocomotionSystem>();

        EnsureXROrigin();

        FindAndCacheControllers();

        CacheAndHideSlots();
        cameraFader?.SetTransparent();
    }

    void Start()
    {
        gameRecordModel = this.GetModel<GameRecordModel>();
        turnoutDatabaseManager = this.GetUtility<TurnoutDatabaseManager>();

        FindAndCacheControllers();

        SetControllersVisible(false);
    }

    private void FindAndCacheControllers()
    {
        bool foundNew = false;

        if (leftController == null && !string.IsNullOrEmpty(leftControllerTag))
        {
            GameObject[] leftControllers = GameObject.FindGameObjectsWithTag(leftControllerTag);
            leftController = leftControllers.FirstOrDefault(go => go.activeInHierarchy);

            if (leftController != null)
            {
                leftControllerOriginalScale = leftController.transform.localScale;
                if (leftControllerOriginalScale == Vector3.zero)
                    leftControllerOriginalScale = Vector3.one;

                foundNew = true;
                Debug.Log($"[VRSceneManager] 缓存左控制器: {leftController.name}, 缩放: {leftControllerOriginalScale}");
            }
        }

        if (rightController == null && !string.IsNullOrEmpty(rightControllerTag))
        {
            GameObject[] rightControllers = GameObject.FindGameObjectsWithTag(rightControllerTag);
            rightController = rightControllers.FirstOrDefault(go => go.activeInHierarchy);

            if (rightController != null)
            {
                rightControllerOriginalScale = rightController.transform.localScale;
                if (rightControllerOriginalScale == Vector3.zero)
                    rightControllerOriginalScale = Vector3.one;

                foundNew = true;
                Debug.Log($"[VRSceneManager] 缓存右控制器: {rightController.name}, 缩放: {rightControllerOriginalScale}");
            }
        }

        if (!foundNew && !ControllersCached)
        {
            Debug.LogWarning("[VRSceneManager] 未找到任何控制器，将在使用时重试");
        }
    }

    private void ForceRefreshControllers()
    {
        leftController = null;
        rightController = null;
        FindAndCacheControllers();
    }

    private void SetControllersVisible(bool visible)
    {
        if (!ControllersCached)
        {
            FindAndCacheControllers();
        }

        if (!ControllersCached && gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedSetVisible(visible));
            return;
        }

        Vector3 targetScale = visible ? leftControllerOriginalScale : Vector3.zero;

        if (leftController != null)
        {
            leftController.transform.localScale = targetScale;
            Debug.Log($"[VRSceneManager] 左控制器 {(visible ? "显示" : "隐藏")}，缩放: {targetScale}");
        }

        if (rightController != null)
        {
            rightController.transform.localScale = visible ? rightControllerOriginalScale : Vector3.zero;
            Debug.Log($"[VRSceneManager] 右控制器 {(visible ? "显示" : "隐藏")}");
        }
    }

    private IEnumerator DelayedSetVisible(bool visible)
    {
        yield return null;
        FindAndCacheControllers();
        SetControllersVisible(visible);
    }

    public async Task LoadSceneAsync(string sceneName, bool setActive = true, bool showLoading = true)
    {
        if (isLoading) return;
        if (IsSceneLoaded(sceneName)) return;

        isLoading = true;

        ForceRefreshControllers();
        SetControllersVisible(false);

        if (disableMovementDuringLoading)
        {
            SetLocomotionEnabled(false);
        }

        try
        {
            if (showLoading)
            {
                cameraFader?.SetTransparent();
                loadingScreen?.ShowImmediate();
                Debug.Log($"[VRSceneManager] 开始加载: {sceneName}");
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"[VRSceneManager] 场景 {sceneName} 加载失败");
                return;
            }

            operation.allowSceneActivation = false;
            float lastProgress = 0f;

            while (operation.progress < 0.9f)
            {
                float currentProgress = operation.progress / 0.9f;
                if (currentProgress > lastProgress)
                {
                    lastProgress = currentProgress;
                    loadingScreen?.SetProgress(currentProgress);
                }

                await Task.Yield();
            }

            loadingScreen?.SetProgress(0.9f);
            operation.allowSceneActivation = true;

            float fakeProgress = 0.9f;
            while (!operation.isDone)
            {
                fakeProgress += Time.deltaTime * 0.3f;
                if (fakeProgress > 0.99f) fakeProgress = 0.99f;
                loadingScreen?.SetProgress(fakeProgress);
                await Task.Yield();
            }

            loadingScreen?.SetProgress(1f);

            if (showLoading)
            {
                cameraFader?.SetBlack();
                for (int i = 0; i < blackScreenFrames; i++)
                    await Task.Yield();
            }

            Scene newScene = SceneManager.GetSceneByName(sceneName);
            if (newScene.IsValid())
            {
                loadedScenes.Add(newScene);
            }

            OnSceneLoaded?.Invoke(sceneName);

            if (newScene.IsValid() && setActive)
            {
                SceneManager.SetActiveScene(newScene);
            }

            if (autoMoveToSpawnPoint && xrOrigin != null && newScene.IsValid())
            {
                var moveTask = MoveXROriginToSceneStartPointAsync(newScene);
                await moveTask;
            }

            // 🔥 优化：删除 Physics.SyncTransforms()，依赖 autoSyncTransforms 自动同步
            await Task.Yield();
            await Task.Yield();
            // Physics.SyncTransforms(); // 已删除
            await Task.Yield();

            ChooseWaistSlotsByProjectId();
            CheckAndApplyMovementRestriction(sceneName);
            
            SetControllersVisible(true);

            await Task.Yield();
            await Task.Yield();

            if (showLoading)
            {
                var fadeTask = FadeFromBlack();
                loadingScreen?.HideImmediate();
                await fadeTask;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"加载场景失败: {e.Message}");
            throw;
        }
        finally
        {
            isLoading = false;

            if (turnoutWaistSlots != null && turnoutWaistSlots.transform.localScale == Vector3.zero)
            {
                Debug.LogWarning("[VRSceneManager] 异常保护：重新显示 Slots");
                ChooseWaistSlotsByProjectId();
            }
            
            if (!ControllersCached)
            {
                Debug.LogWarning("[VRSceneManager] 异常保护：尝试查找控制器");
                FindAndCacheControllers();
            }
            if (leftController != null && leftController.transform.localScale == Vector3.zero)
            {
                leftController.transform.localScale = leftControllerOriginalScale;
            }
            if (rightController != null && rightController.transform.localScale == Vector3.zero)
            {
                rightController.transform.localScale = rightControllerOriginalScale;
            }

            if (disableMovementDuringLoading)
            {
                if (isMovementRestricted)
                {
                    SetLocomotionEnabled(false);
                    Debug.Log("[VRSceneManager] 加载完成，MenuScene 中移动受限");
                }
                else
                {
                    SetLocomotionEnabled(true);
                    Debug.Log("[VRSceneManager] 加载完成，恢复移动");
                }
            }

            // 🔥 优化：延迟异步卸载资源，不阻塞场景显示
            _ = UnloadUnusedAssetsAsync();
        }
    }

    public async Task SwitchSceneAsync(string newSceneName)
    {
        if (isLoading) return;
        isLoading = true;

        ForceRefreshControllers();
        SetControllersVisible(false);
        SetLocomotionEnabled(false);
        
        HideSlots();

        try
        {
            cameraFader?.SetTransparent();
            loadingScreen?.ShowImmediate();

            for (int i = loadedScenes.Count - 1; i >= 0; i--)
            {
                if (loadedScenes[i].IsValid())
                {
                    string sceneName = loadedScenes[i].name;
                    AsyncOperation op = SceneManager.UnloadSceneAsync(loadedScenes[i]);
                    while (!op.isDone) await Task.Yield();
                    OnSceneUnloaded?.Invoke(sceneName);
                }
            }

            loadedScenes.Clear();
            await Task.Yield();

            AsyncOperation operation = SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"[VRSceneManager] 场景 {newSceneName} 加载失败！");
                return;
            }

            operation.allowSceneActivation = false;
            float lastProgress = 0f;

            while (operation.progress < 0.9f)
            {
                float currentProgress = operation.progress / 0.9f;
                if (currentProgress > lastProgress)
                {
                    lastProgress = currentProgress;
                    loadingScreen?.SetProgress(currentProgress);
                }

                await Task.Yield();
            }

            loadingScreen?.SetProgress(0.9f);
            operation.allowSceneActivation = true;

            float fakeProgress = 0.9f;
            while (!operation.isDone)
            {
                fakeProgress += Time.deltaTime * 0.3f;
                if (fakeProgress > 0.99f) fakeProgress = 0.99f;
                loadingScreen?.SetProgress(fakeProgress);
                await Task.Yield();
            }

            loadingScreen?.SetProgress(1f);

            cameraFader?.SetBlack();
            for (int i = 0; i < blackScreenFrames; i++)
                await Task.Yield();

            Scene newScene = SceneManager.GetSceneByName(newSceneName);
            if (newScene.IsValid())
            {
                loadedScenes.Add(newScene);
                SceneManager.SetActiveScene(newScene);
            }

            OnSceneLoaded?.Invoke(newSceneName);
            EnsureXROrigin();

            if (autoMoveToSpawnPoint && xrOrigin != null && newScene.IsValid())
            {
                var moveTask = MoveXROriginToSceneStartPointAsync(newScene);
                await moveTask;
            }

            // 🔥 优化：删除 Physics.SyncTransforms()，依赖 autoSyncTransforms 自动同步
            await Task.Yield();
            await Task.Yield();
            // Physics.SyncTransforms(); // 已删除
            await Task.Yield();

            ChooseWaistSlotsByProjectId();
            CheckAndApplyMovementRestriction(newSceneName);
            
            SetControllersVisible(true);

            await Task.Yield();
            await Task.Yield();

            var fadeTask = FadeFromBlack();
            loadingScreen?.HideImmediate();
            await fadeTask;

            // 🔥 优化：删除 Resources.UnloadUnusedAssets() 同步调用
            // Resources.UnloadUnusedAssets(); // 已删除
        }
        catch (Exception e)
        {
            Debug.LogError($"切换场景失败: {e.Message}");
            throw;
        }
        finally
        {
            isLoading = false;

            if (turnoutWaistSlots != null && turnoutWaistSlots.transform.localScale == Vector3.zero)
            {
                Debug.LogWarning("[VRSceneManager] 异常保护：重新显示 Slots");
                ChooseWaistSlotsByProjectId();
            }
            
            if (!ControllersCached)
            {
                Debug.LogWarning("[VRSceneManager] 异常保护：尝试查找控制器");
                FindAndCacheControllers();
            }
            if (leftController != null && leftController.transform.localScale == Vector3.zero)
            {
                leftController.transform.localScale = leftControllerOriginalScale;
            }
            if (rightController != null && rightController.transform.localScale == Vector3.zero)
            {
                rightController.transform.localScale = rightControllerOriginalScale;
            }

            if (isMovementRestricted)
            {
                SetLocomotionEnabled(false);
                Debug.Log("[VRSceneManager] 切换完成，移动受限");
            }
            else
            {
                SetLocomotionEnabled(true);
                Debug.Log("[VRSceneManager] 切换完成，恢复移动");
            }

            // 🔥 优化：延迟异步卸载资源，不阻塞场景显示
            _ = UnloadUnusedAssetsAsync();
        }
    }

    // 🔥 新增：异步卸载未使用资源，不阻塞主线程
    private async Task UnloadUnusedAssetsAsync()
    {
        if (_isUnloading) return;
        
        _isUnloading = true;
        
        // 延迟两帧，确保场景切换的视觉反馈已完成
        await Task.Yield();
        await Task.Yield();
        
        _pendingUnloadOperation = Resources.UnloadUnusedAssets();
        
        // 分帧等待，每帧检查进度，不阻塞渲染
        while (_pendingUnloadOperation != null && !_pendingUnloadOperation.isDone)
        {
            await Task.Yield();
        }
        
        _pendingUnloadOperation = null;
        _isUnloading = false;
        
        Debug.Log("[VRSceneManager] 异步资源卸载完成");
    }

    // 🔥 新增：在场景完全稳定后触发卸载（可在MenuScene停留时调用）
    public void RequestLazyUnload()
    {
        StartCoroutine(LazyUnloadCoroutine());
    }

    private IEnumerator LazyUnloadCoroutine()
    {
        yield return new WaitForSeconds(3f);
        
        var op = Resources.UnloadUnusedAssets();
        while (!op.isDone)
        {
            yield return null;
        }
        
        Debug.Log("[VRSceneManager] 延迟卸载完成");
    }

    private async Task MoveXROriginToSceneStartPointAsync(Scene scene)
    {
        if (xrOrigin == null || !scene.IsValid()) return;

        Transform spawnPoint = FindSpawnPointInScene(scene);

        if (spawnPoint != null)
        {
            Debug.Log($"[VRSceneManager] 找到StartPoint: {spawnPoint.position}，准备使用协程移动");

            var tcs = new TaskCompletionSource<object>();
            StartCoroutine(MoveXROriginCoroutine(spawnPoint, tcs));
            await tcs.Task;
        }
        else
        {
            Debug.LogWarning($"[VRSceneManager] 场景 {scene.name} 中未找到StartPoint (Tag: {spawnPointTag})");
        }
    }

    // 🔥 优化：减少 yield 次数，从8次压缩到3次
    private IEnumerator MoveXROriginCoroutine(Transform targetPoint, TaskCompletionSource<object> tcs)
    {
        if (xrOrigin == null || targetPoint == null)
        {
            tcs.TrySetResult(null);
            yield break;
        }

        Transform xrSetup = xrOrigin.parent;
        XROrigin xrOriginComp = xrOrigin.GetComponent<XROrigin>();
        CharacterController charCtrl = xrOrigin.GetComponent<CharacterController>();

        Debug.Log($"[VRSceneManager] 协程开始: 当前XROrigin位置={xrOrigin.position}, 目标={targetPoint.position}");

        // 步骤1：禁用组件
        if (charCtrl != null) charCtrl.enabled = false;
        if (xrOriginComp != null) xrOriginComp.enabled = false;
        
        // 步骤2：重置本地变换
        xrOrigin.localPosition = Vector3.zero;
        xrOrigin.localRotation = Quaternion.identity;
        xrOrigin.localScale = Vector3.one;
        
        yield return null;  // 等待一帧让变更生效

        // 步骤3：设置世界位置
        if (xrSetup != null)
        {
            xrSetup.position = targetPoint.position - xrOrigin.localPosition;
            xrSetup.rotation = targetPoint.rotation;
        }
        else
        {
            xrOrigin.position = targetPoint.position;
            xrOrigin.rotation = targetPoint.rotation;
        }
        
        yield return null;  // 等待位置生效

        // 步骤4：重新启用组件
        if (xrOriginComp != null) xrOriginComp.enabled = true;
        if (charCtrl != null)
        {
            charCtrl.transform.SetPositionAndRotation(xrOrigin.position, xrOrigin.rotation);
            charCtrl.enabled = true;
        }
        
        yield return null;  // 最终同步

        Debug.Log($"[VRSceneManager] 协程完成: XROrigin最终位置={xrOrigin.position}");
        tcs.TrySetResult(null);
    }

    private void EnsureXROrigin()
    {
        Transform previousXrOrigin = xrOrigin;

        if (xrOrigin == null)
        {
            var xro = FindObjectOfType<XROrigin>();
            if (xro != null)
            {
                xrOrigin = xro.transform;
                Debug.Log("[VRSceneManager] 找到 XROrigin");
            }
            else
            {
                Debug.LogError("[VRSceneManager] 未找到 XROrigin！");
            }
        }

        if (xrOrigin != previousXrOrigin && xrOrigin != null)
        {
            OnXROriginChanged?.Invoke(xrOrigin);
        }
    }

    private void CheckAndApplyMovementRestriction(string sceneName)
    {
        if (autoRestrictInMenu && restrictMovementScenes.Contains(sceneName))
        {
            Debug.Log($"[VRSceneManager] {sceneName} 中限制移动");
            isMovementRestricted = true;
        }
        else
        {
            isMovementRestricted = false;
        }
    }

    public void ReleaseMovementRestriction()
    {
        if (isMovementRestricted)
        {
            Debug.Log("[VRSceneManager] 解除移动限制");
            SetLocomotionEnabled(true);
            isMovementRestricted = false;
        }
    }

    public void ForceMovementRestriction()
    {
        Debug.Log("[VRSceneManager] 强制限制移动");
        SetLocomotionEnabled(false);
        isMovementRestricted = true;
    }

    private async Task FadeFromBlack()
    {
        if (cameraFader == null) return;

        var tcs = new TaskCompletionSource<object>();
        var tween = cameraFader.FadeIn(fadeInDuration);

        if (tween != null)
        {
            tween.OnComplete(() => tcs.TrySetResult(null));
            await tcs.Task;
        }
        else
        {
            await Task.Delay((int)(fadeInDuration * 1000));
        }
    }

    private void SetLocomotionEnabled(bool enabled)
    {
        if (locomotionSystem == null)
        {
            locomotionSystem = FindObjectOfType<LocomotionSystem>();
            if (locomotionSystem == null)
            {
                Debug.LogWarning("[VRSceneManager] 未找到 LocomotionSystem");
                return;
            }
        }

        locomotionSystem.enabled = enabled;
        Debug.Log($"[VRSceneManager] LocomotionSystem {(enabled ? "启用" : "禁用")}");
    }

    private Transform FindSpawnPointInScene(Scene scene)
    {
        if (!scene.IsValid()) return null;

        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (var root in rootObjects)
        {
            if (root.CompareTag(spawnPointTag))
                return root.transform;

            Transform found = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.CompareTag(spawnPointTag));

            if (found != null)
                return found;
        }

        return null;
    }

    private bool IsSceneLoaded(string sceneName) =>
        loadedScenes.Exists(s => s.name == sceneName);

    public bool IsMovementRestricted => isMovementRestricted;

    private void CacheAndHideSlots()
    {
        if (turnoutWaistSlots == null) return;

        slotsOriginalScale = turnoutWaistSlots.transform.localScale;
        slotsScaleCached = true;

        turnoutWaistSlots.transform.localScale = Vector3.zero;

        Debug.Log($"[VRSceneManager] Slots 原始缩放已缓存: {slotsOriginalScale}，初始隐藏");
    }

    private void ChooseWaistSlotsByProjectId()
    {
        if (turnoutWaistSlots == null) return;

        string projectName = turnoutDatabaseManager.GetProjectName(gameRecordModel.currentprojectId);
        
        if (projectName != null)
        {
            Debug.Log("ProjectName:" + projectName);
            bool shouldShow = projectName.Contains(ProjectName.人工准备进路.ToString());

            if (shouldShow)
            {
                ShowSlots();
                Debug.Log($"[VRSceneManager] Slots 已显示（缩放恢复）: {slotsOriginalScale}");
            }
            else
            {
                HideSlots();
                Debug.Log("[VRSceneManager] Slots 已隐藏（缩放为0），项目不匹配");
            }
        }
        else
        {
            HideSlots();
            Debug.Log("[VRSceneManager] Slots 已隐藏（缩放为0），项目名为空");
        }
    }

    private void ShowSlots()
    {
        if (turnoutWaistSlots == null) return;

        if (xrOrigin != null && turnoutWaistSlots.transform.parent != xrOrigin)
        {
            turnoutWaistSlots.transform.SetParent(xrOrigin, false);
        }

        if (slotsScaleCached)
        {
            turnoutWaistSlots.transform.localScale = slotsOriginalScale;
        }
        else
        {
            turnoutWaistSlots.transform.localScale = Vector3.one;
        }
    }

    private void HideSlots()
    {
        if (turnoutWaistSlots == null) return;
        turnoutWaistSlots.transform.localScale = Vector3.zero;
    }

    public IArchitecture GetArchitecture()
    {
        return RailGameApp.Interface;
    }
}