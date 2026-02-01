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

public class VRSceneManager : MonoBehaviour
{
    public static VRSceneManager Instance { get; private set; }

    [Header("Scene References")] 
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private CameraFade cameraFader;
    [SerializeField] private LoadingScreen loadingScreen;

    [Header("XR Locomotion Control")] 
    [SerializeField] private LocomotionSystem locomotionSystem;
    [SerializeField] private bool disableMovementDuringLoading = true;

    [Header("场景特定移动限制")]
    [SerializeField] private List<string> restrictMovementScenes = new List<string>() { "MenuScene", "MainMenu" };
    [SerializeField] private bool autoRestrictInMenu = true;
    
    [Header("Spawn Point Settings")] 
    [SerializeField] private string spawnPointTag = "StartPoint";
    [SerializeField] private bool autoMoveToSpawnPoint = true;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.8f;     
    [SerializeField] private int blackScreenFrames = 2;       
    
    [Header("Controller Settings")]
    [SerializeField] private string leftControllerTag = "LeftController";
    [SerializeField] private string rightControllerTag = "RightController";
    [SerializeField] private string waistSlotsTag = "WaistSlots";
    
    private List<Scene> loadedScenes = new List<Scene>();
    private bool isLoading = false;
    private bool wasLocomotionEnabled = true;
    private bool isMovementRestricted = false;
    
    // 缓存控制器引用
    private GameObject leftController;
    private GameObject rightController;
    private List<GameObject> waistSlots = new List<GameObject>();
    private bool hasCachedObjects = false;

    public static event Action<Transform> OnXROriginChanged;
    
    public event Action<string> OnSceneLoaded;
    public event Action<string> OnSceneUnloaded;

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
        
        // 尝试查找，但可能找不到（如果对象还没加载）
        FindAndCacheAllObjects();
        
        cameraFader?.SetTransparent();
    }

    void Start()
    {
        // Start 中再次尝试查找，此时 Persistent 场景的对象应该已经加载
        if (!hasCachedObjects)
        {
            FindAndCacheAllObjects();
        }
    }

    /// <summary>
    /// 查找并缓存所有需要在加载时隐藏的对象
    /// </summary>
    private void FindAndCacheAllObjects()
    {
        bool foundAny = false;

        // 查找左控制器
        if (leftController == null && !string.IsNullOrEmpty(leftControllerTag))
        {
            GameObject[] leftControllers = GameObject.FindGameObjectsWithTag(leftControllerTag);
            if (leftControllers.Length > 0)
            {
                leftController = leftControllers[0];
                foundAny = true;
                Debug.Log($"[VRSceneManager] 缓存左控制器: {leftController.name}");
            }
        }

        // 查找右控制器
        if (rightController == null && !string.IsNullOrEmpty(rightControllerTag))
        {
            GameObject[] rightControllers = GameObject.FindGameObjectsWithTag(rightControllerTag);
            if (rightControllers.Length > 0)
            {
                rightController = rightControllers[0];
                foundAny = true;
                Debug.Log($"[VRSceneManager] 缓存右控制器: {rightController.name}");
            }
        }

        // 查找所有WaistSlots（可能有多个）
        if (waistSlots.Count == 0 && !string.IsNullOrEmpty(waistSlotsTag))
        {
            GameObject[] slots = GameObject.FindGameObjectsWithTag(waistSlotsTag);
            if (slots.Length > 0)
            {
                waistSlots.Clear();
                waistSlots.AddRange(slots);
                foundAny = true;
                Debug.Log($"[VRSceneManager] 缓存了 {waistSlots.Count} 个WaistSlots (Tag: {waistSlotsTag})");
            }
        }

        if (foundAny)
        {
            hasCachedObjects = true;
        }
    }

    /// <summary>
    /// 确保缓存已初始化，如果没有则尝试查找
    /// </summary>
    private void EnsureCacheInitialized()
    {
        if (!hasCachedObjects || (leftController == null && rightController == null && waistSlots.Count == 0))
        {
            Debug.Log("[VRSceneManager] 缓存未初始化或已失效，重新查找...");
            FindAndCacheAllObjects();
        }
    }

    /// <summary>
    /// 设置所有控制器和WaistSlots的显示状态
    /// </summary>
    private void SetControllersAndSlotsVisible(bool visible)
    {
        // 确保缓存已初始化
        EnsureCacheInitialized();

        // 设置左控制器
        if (leftController != null)
        {
            leftController.SetActive(visible);
            Debug.Log($"[VRSceneManager] 左控制器 {(visible ? "显示" : "隐藏")}");
        }
        else
        {
            Debug.LogWarning("[VRSceneManager] 左控制器引用为null");
        }

        // 设置右控制器
        if (rightController != null)
        {
            rightController.SetActive(visible);
            Debug.Log($"[VRSceneManager] 右控制器 {(visible ? "显示" : "隐藏")}");
        }
        else
        {
            Debug.LogWarning("[VRSceneManager] 右控制器引用为null");
        }

        // 设置所有WaistSlots
        int processedCount = 0;
        foreach (var slot in waistSlots)
        {
            if (slot != null)
            {
                slot.SetActive(visible);
                processedCount++;
            }
        }
        
        if (waistSlots.Count > 0)
        {
            Debug.Log($"[VRSceneManager] {processedCount}/{waistSlots.Count} 个WaistSlots {(visible ? "显示" : "隐藏")}");
        }
    }

    public async Task LoadSceneAsync(string sceneName, bool setActive = true, bool showLoading = true)
    {
        if (isLoading) return;
        if (IsSceneLoaded(sceneName)) return;

        isLoading = true;

        // 隐藏控制器和WaistSlots
        SetControllersAndSlotsVisible(false);

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

            CheckAndApplyMovementRestriction(sceneName);

            if (showLoading)
            {
                var fadeTask = FadeFromBlack();
                loadingScreen?.HideImmediate();
                await fadeTask;
                Resources.UnloadUnusedAssets();
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
            
            // 显示控制器和WaistSlots
            SetControllersAndSlotsVisible(true);
            
            if (disableMovementDuringLoading)
            {
                if (isMovementRestricted)
                {
                    Debug.Log("[VRSceneManager] 加载完成，MenuScene 中移动受限");
                }
                else
                {
                    SetLocomotionEnabled(true);
                }
            }
        }
    }

    public async Task SwitchSceneAsync(string newSceneName)
    {
        if (isLoading) return;
        isLoading = true;

        // 隐藏控制器和WaistSlots
        SetControllersAndSlotsVisible(false);
        
        SetLocomotionEnabled(false);

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

            CheckAndApplyMovementRestriction(newSceneName);

            var fadeTask = FadeFromBlack();
            loadingScreen?.HideImmediate();
            await fadeTask;
            
            Resources.UnloadUnusedAssets();
        }
        catch (Exception e)
        {
            Debug.LogError($"切换场景失败: {e.Message}");
            throw;
        }
        finally
        {
            isLoading = false;
            
            // 显示控制器和WaistSlots
            SetControllersAndSlotsVisible(true);
            
            if (isMovementRestricted)
            {
                SetLocomotionEnabled(false);
            }
            else
            {
                SetLocomotionEnabled(true);
            }
        }
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

        Debug.Log($"[VRSceneManager] 协程开始: 当前XROrigin位置={xrOrigin.position}, 本地={xrOrigin.localPosition}, 目标={targetPoint.position}");

        // 第1步: 禁用所有可能干扰位置的组件
        if (charCtrl != null) charCtrl.enabled = false;
        if (xrOriginComp != null) xrOriginComp.enabled = false;

        yield return null;

        // 第2步: 【关键】强制重置XROrigin本地变换为绝对零（解决MenuScene偏移问题）
        xrOrigin.localPosition = Vector3.zero;
        xrOrigin.localRotation = Quaternion.identity;
        xrOrigin.localScale = Vector3.one;
        
        // 强制刷新
        xrOrigin.GetComponent<Transform>();
        
        yield return null;
        
        Debug.Log($"[VRSceneManager] 重置本地变换后: XROrigin世界={xrOrigin.position}, 本地={xrOrigin.localPosition}");

        // 第3步: 【关键】计算父物体应该的位置，使得XROrigin世界位置等于targetPoint
        if (xrSetup != null)
        {
            // 父物体位置 = 目标位置 - XROrigin本地位置（应该是零）
            // 但为了保险，还是计算一下
            Vector3 setupTargetPos = targetPoint.position - xrOrigin.localPosition;
            xrSetup.position = setupTargetPos;
            xrSetup.rotation = targetPoint.rotation;
            
            // 强制刷新
            xrSetup.GetComponent<Transform>();
            
            yield return null;
            
            Debug.Log($"[VRSceneManager] 移动父物体后: 父物体={xrSetup.position}, XROrigin={xrOrigin.position}");
            
            // 第4步: 【保险】如果还有偏差，直接强制设置
            if (Vector3.Distance(xrOrigin.position, targetPoint.position) > 0.01f)
            {
                Debug.LogWarning($"[VRSceneManager] 位置有偏差，强制修正: {Vector3.Distance(xrOrigin.position, targetPoint.position)}");
                
                // 重新计算父物体位置
                xrSetup.position = targetPoint.position - xrOrigin.localPosition;
                xrSetup.GetComponent<Transform>();
                
                yield return null;
            }
        }
        else
        {
            // 没有父物体，直接移动
            xrOrigin.position = targetPoint.position;
            xrOrigin.rotation = targetPoint.rotation;
            
            yield return null;
            
            Debug.Log($"[VRSceneManager] 直接移动XROrigin到: {xrOrigin.position}");
        }

        // 第5步: 强制同步物理系统
        Physics.SyncTransforms();
        yield return null;

        // 第6步: 重新启用组件
        if (xrOriginComp != null) xrOriginComp.enabled = true;
        
        if (charCtrl != null) 
        {
            charCtrl.transform.position = xrOrigin.position;
            charCtrl.transform.rotation = xrOrigin.rotation;
            charCtrl.enabled = true;
        }

        yield return null;

        Debug.Log($"[VRSceneManager] 协程完成: XROrigin最终位置={xrOrigin.position}, 目标={targetPoint.position}, 差值={Vector3.Distance(xrOrigin.position, targetPoint.position)}");

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
            SetLocomotionEnabled(false);
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
        if (locomotionSystem == null) return;
        
        if (!enabled)
        {
            wasLocomotionEnabled = locomotionSystem.enabled;
            locomotionSystem.enabled = false;
        }
        else
        {
            locomotionSystem.enabled = wasLocomotionEnabled;
        }
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
}