using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using System.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;

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

        if (xrOrigin == null)
        {
            var xro = FindObjectOfType<XROrigin>();
            if (xro != null) xrOrigin = xro.transform;
        }
        
        cameraFader?.SetTransparent();
    }

    public async Task LoadSceneAsync(string sceneName, bool setActive = true, bool showLoading = true)
    {
        if (isLoading) return;
        if (IsSceneLoaded(sceneName)) return;

        isLoading = true;
        Transform targetSpawnPoint = null;

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

            if (autoMoveToSpawnPoint && xrOrigin != null && newScene.IsValid())
            {
                targetSpawnPoint = FindSpawnPointInScene(newScene);
                if (targetSpawnPoint != null)
                {
                    MoveXROriginToPoint(targetSpawnPoint);
                    await Task.Yield();
                }
            }

            if (newScene.IsValid() && setActive)
            {
                SceneManager.SetActiveScene(newScene);
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

            if (autoMoveToSpawnPoint && xrOrigin != null)
            {
                var targetSpawnPoint = FindSpawnPointInScene(newScene);
                if (targetSpawnPoint != null)
                {
                    MoveXROriginToPoint(targetSpawnPoint);
                    await Task.Yield();
                }
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
            
            var found = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.CompareTag(spawnPointTag));  // 【修复】改为大写 F
            
            if (found != null) return found;
        }
        return null;
    }

    private void MoveXROriginToPoint(Transform targetPoint)
    {
        if (xrOrigin == null || targetPoint == null) return;

        CharacterController charCtrl = xrOrigin.GetComponent<CharacterController>();
        bool wasEnabled = false;
        
        if (charCtrl != null && charCtrl.enabled)
        {
            wasEnabled = true;
            charCtrl.enabled = false;
        }

        var xrOriginComp = xrOrigin.GetComponent<XROrigin>();
        if (xrOriginComp != null)
        {
            xrOriginComp.MoveCameraToWorldLocation(targetPoint.position);
        }
        else
        {
            xrOrigin.position = targetPoint.position;
        }
        
        xrOrigin.rotation = targetPoint.rotation;

        if (wasEnabled && charCtrl != null)
            charCtrl.enabled = true;
    }

    private bool IsSceneLoaded(string sceneName) => 
        loadedScenes.Exists(s => s.name == sceneName);
    
    public bool IsMovementRestricted => isMovementRestricted;
}