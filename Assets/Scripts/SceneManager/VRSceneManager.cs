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
    
    private List<Scene> loadedScenes = new List<Scene>();
    private bool isLoading = false;
    private bool wasLocomotionEnabled = true;
    private bool isMovementRestricted = false;

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
        
        cameraFader?.SetTransparent();
    }

    public async Task LoadSceneAsync(string sceneName, bool setActive = true, bool showLoading = true)
    {
        if (isLoading) return;
        if (IsSceneLoaded(sceneName)) return;

        isLoading = true;

        if (disableMovementDuringLoading)
        {
            SetLocomotionEnabled(false);
            SetInteractionsEnabled(false);
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
            if (disableMovementDuringLoading)
            {
                if (isMovementRestricted)
                {
                    SetInteractionsEnabled(true);
                    Debug.Log("[VRSceneManager] 加载完成，MenuScene 中移动受限但交互可用");
                }
                else
                {
                    SetLocomotionEnabled(true);
                    SetInteractionsEnabled(true);
                }
            }
        }
    }

    public async Task SwitchSceneAsync(string newSceneName)
    {
        if (isLoading) return;
        isLoading = true;

        SetLocomotionEnabled(false);
        SetInteractionsEnabled(false);

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
            if (isMovementRestricted)
            {
                SetInteractionsEnabled(true);
                SetLocomotionEnabled(false);
            }
            else
            {
                SetLocomotionEnabled(true);
                SetInteractionsEnabled(true);
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
            Debug.Log($"[VRSceneManager] {sceneName} 中限制移动，但允许交互");
            SetLocomotionEnabled(false);
            SetInteractionsEnabled(true);
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
        SetInteractionsEnabled(true);
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

    private void SetInteractionsEnabled(bool enabled)
    {
        var rayInteractors = FindObjectsOfType<XRRayInteractor>(true);
        foreach (var interactor in rayInteractors)
        {
            interactor.enabled = enabled;
        }
        
        var directInteractors = FindObjectsOfType<XRDirectInteractor>(true);
        foreach (var interactor in directInteractors)
        {
            interactor.enabled = enabled;
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