using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Manager���� �ʱ�ȭ�ϴ� ��Ʈ��Ʈ�� Ŭ����
/// �ʱ�ȭ ���� ����� ����
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
    /// �Ŵ������� ������� �ʱ�ȭ
    /// </summary>
    private void InitializeManagers()
    {
        // NetworkManager �ʱ�ȭ
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
        ngo.NetworkConfig.PlayerPrefab = null; // ���߿� �÷��̾� ������ �Ҵ�

        // ResourceManager �ʱ�ȭ
        PromoteOrCreate<ResourceManager>("ResourceManager");
        // SceneLoadManager �ʱ�ȭ
        PromoteOrCreate<SceneLoadManager>("SceneLoadManager");
        // GameManager �ʱ�ȭ
        PromoteOrCreate<GameManager>("GameManager");
        // UIManager �ʱ�ȭ
        PromoteOrCreate<UIManager>("UIManager");
        // SoundManager �ʱ�ȭ
        PromoteOrCreate<SoundManager>("SoundManager");
        // InputManager �ʱ�ȭ
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
