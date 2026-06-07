using UnityEngine;
using System.Threading.Tasks;

public class PersistentBootstrap : MonoBehaviour
{
    [SerializeField] private string startSceneName = "MenuScene";
    [SerializeField] private float maxWaitSeconds = 5f;
    
    async void Start()
    {
        // 等待 VRSceneManager 初始化（带超时保护）
        float elapsed = 0f;
        while (VRSceneManager.Instance == null && elapsed < maxWaitSeconds)
        {
            await Task.Yield();
            elapsed += Time.deltaTime;
        }
        
        if (VRSceneManager.Instance == null)
        {
            Debug.LogError("[PersistentBootstrap] VRSceneManager 初始化超时！");
            return;
        }
        
        // 额外等待一帧确保所有 Awake 完成
        await Task.Yield();
        
        await VRSceneManager.Instance.LoadSceneAsync(startSceneName);
    }
}