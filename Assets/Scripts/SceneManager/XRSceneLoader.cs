using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit; // 2.5.3 命名空间

public class XRSceneLoader : MonoBehaviour
{
    public static XRSceneLoader Instance { get; private set; }
    public XROrigin xrOrigin; // StarterAssets 里自带
    public float minLoadTime = 1.2f; // 防止 VR 闪屏
    public bool pauseLocomotion = true; // 加载时是否冻结传送

    public static event Action<string> OnStart;
    public static event Action<float> OnProgress;
    public static event Action<string> OnComplete;

    private AsyncOperation op;
    private bool loading;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /* ===== 业务层只调这两个 ===== */
    public void LoadSingle(string sceneName) => StartCoroutine(IE_Load(sceneName, LoadSceneMode.Single));
    public void LoadAdditive(string sceneName) => StartCoroutine(IE_Load(sceneName, LoadSceneMode.Additive));

    /* =========================== */
    private IEnumerator IE_Load(string sceneName, LoadSceneMode mode)
    {
        if (loading) yield break;
        loading = true;
        OnStart?.Invoke(sceneName);

        if (pauseLocomotion) XRLocomotionPause.Instance.Pause(); // 冻结传送

        yield return Resources.UnloadUnusedAssets();
        GC.Collect();

        op = SceneManager.LoadSceneAsync(sceneName, mode);
        op.allowSceneActivation = false;

        float timer = 0f;
        while (!op.isDone)
        {
            timer += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(op.progress / 0.9f);
            OnProgress?.Invoke(p);

            if (op.progress >= 0.9f && timer >= minLoadTime)
                op.allowSceneActivation = true;

            yield return null;
        }

        if (pauseLocomotion) XRLocomotionPause.Instance.Resume();
        OnComplete?.Invoke(sceneName);
        loading = false;
    }
}