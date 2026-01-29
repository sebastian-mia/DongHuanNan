using UnityEngine;
using System.Threading.Tasks;

public class PersistentBootstrap : MonoBehaviour
{
    [SerializeField] private string startSceneName = "MenuScene";
    
    async void Start()
    {
        // 确保其他Manager已初始化
        await Task.Yield();
        
        // 加载初始场景
        if (VRSceneManager.Instance != null)
        {
            await VRSceneManager.Instance.LoadSceneAsync(startSceneName);
        }
    }
}