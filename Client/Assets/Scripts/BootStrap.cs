using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Manager들을 초기화하는 부트스트랩 클래스
/// 초기화 순서 명시적 제어
/// 1. NetworkManager
/// 2. ResourceManager
/// 3. PoolManager
/// 4. GraphicManager
/// 5. SceneLoadManager
/// 6. UIManager
/// 7. SoundManager
/// 8. InputManager
/// 9. CameraManager
/// 10. StageManager
/// 11. GameManager
/// </summary>
public class BootStrap : MonoBehaviour
{
    private static BootStrap instance;

    #region Unity Methods

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeManagers();
    }

    #endregion Unity Methods

    #region Custom Methods

    /// <summary>
    /// 매니저들을 순서대로 초기화
    /// </summary>
    private void InitializeManagers()
    {
        // 1. NetworkManager 초기화
        NetworkManager ngo = FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
        if (ngo == null)
        {
            GameObject go = new GameObject("NetworkManager");
            ngo = go.AddComponent<NetworkManager>();
            DontDestroyOnLoad(go);
        }

        UnityTransport utp = ngo.GetComponent<UnityTransport>();

        if (utp == null)
        {
            utp = ngo.gameObject.AddComponent<UnityTransport>();
        }

        if (ngo.NetworkConfig == null)
        {
            ngo.NetworkConfig = new NetworkConfig();
        }

        ngo.NetworkConfig.NetworkTransport = utp;

        // 2. ResourceManager 초기화
        PromoteOrCreate<ResourceManager>("ResourceManager");
        // 3. PoolManager 초기화
        PromoteOrCreate<PoolManager>("PoolManager");
        // 4. GraphicManager 초기화
        PromoteOrCreate<GraphicManager>("GraphicManager");
        // 5. SceneLoadManager 초기화
        PromoteOrCreate<SceneLoadManager>("SceneLoadManager");
        // 6. UIManager 초기화
        PromoteOrCreate<UIManager>("UIManager");
        // 7. SoundManager 초기화
        PromoteOrCreate<SoundManager>("SoundManager");
        // 8. InputManager 초기화
        PromoteOrCreate<InputManager>("InputManager");
        // 9. CameraManager 초기화
        PromoteOrCreate<CameraManager>("CameraManager");
        // 10. StageManager 초기화
        PromoteOrCreate<StageManager>("StageManager");
        // 11. GameManager 초기화
        PromoteOrCreate<GameManager>("GameManager");
    }
   private T PromoteOrCreate<T>(string goName) where T : Component
    {
        T existing = FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        GameObject go = new GameObject(goName);
        DontDestroyOnLoad(go);
        return go.AddComponent<T>();
    }
 

    #endregion Custom Methods
}
