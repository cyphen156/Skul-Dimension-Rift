using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Manager들을 초기화하는 부트스트랩 클래스
/// 초기화 순서 명시적 제어
/// 1. NetworkManager
/// 2. ResourceManager
/// 3. SceneLoadManager
/// 4. GameManager
/// 5. UIManager
/// 6. SoundManager
/// 7. InputManager
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
        // NetworkManager 초기화
        var ngo = FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
        if (ngo == null)
        {
            var go = new GameObject("NetworkManager");
            ngo = go.AddComponent<NetworkManager>();
            DontDestroyOnLoad(go);
        }

        var utp = ngo.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();

        if (utp == null)
        {
            utp = ngo.gameObject.AddComponent<UnityTransport>();
        }

        if (ngo.NetworkConfig == null)
        {
            ngo.NetworkConfig = new NetworkConfig();
        }

        ngo.NetworkConfig.NetworkTransport = utp;
            ngo.NetworkConfig.TickRate = 60;
        ngo.NetworkConfig.PlayerPrefab = null; // 나중에 플레이어 프리팹 할당

        // ResourceManager 초기화
        PromoteOrCreate<ResourceManager>("ResourceManager");
        // SceneLoadManager 초기화
        PromoteOrCreate<SceneLoadManager>("SceneLoadManager");
        // GameManager 초기화
        PromoteOrCreate<GameManager>("GameManager");
        // UIManager 초기화
        PromoteOrCreate<UIManager>("UIManager");
        // SoundManager 초기화
        PromoteOrCreate<SoundManager>("SoundManager");
        // InputManager 초기화
        PromoteOrCreate<InputManager>("InputManager");
    }

    private T PromoteOrCreate<T>(string goName) where T : Component
    {
        var existing = FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (existing != null)
        {
            return existing;
        }

        var go = new GameObject(goName);
        DontDestroyOnLoad(go);
        return go.AddComponent<T>();
    }
    #endregion Custom Methods
}
