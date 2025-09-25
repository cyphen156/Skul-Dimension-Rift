using System.Runtime.CompilerServices;
using Unity.Netcode;
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

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManagers();
        }
        else
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// �Ŵ������� ������� �ʱ�ȭ
    /// </summary>
    private void InitializeManagers()
    {
        // NetworkManager �ʱ�ȭ
        var ngo = FindFirstObjectByType<NetworkManager>();
        if (ngo == null)
        {
            var go = new GameObject("NetworkManager");
            ngo = go.AddComponent<NetworkManager>();
            DontDestroyOnLoad(go);
        }

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
        var existing = FindFirstObjectByType<T>();
        if (existing != null) return existing;

        var go = new GameObject(goName);
        DontDestroyOnLoad(go);
        return go.AddComponent<T>();
    }
}
