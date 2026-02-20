using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentReticle : MonoBehaviour
{
    public static PersistentReticle Instance { get; private set; }

    private void Awake()
    {
        // 单例：已存在就销毁新的
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // 移动到主场景（假设主场景是0号）
        var mainScene = SceneManager.GetSceneAt(0);
        if (gameObject.scene != mainScene)
        {
            SceneManager.MoveGameObjectToScene(gameObject, mainScene);
        }
    }
}