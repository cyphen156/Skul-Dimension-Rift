using Assets.Scripts.Content;
using Assets.Scripts.Utility;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
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

    private const string defaultManifestPath = "Data/ContentManifest/";

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

        StartCoroutine(C_InitializeManagers());
    }

    #endregion Unity Methods

    #region Custom Methods

    /// <summary>
    /// 매니저들을 순서대로 초기화
    /// </summary>
    private IEnumerator C_InitializeManagers()
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
        ResourceManager rm = PromoteOrCreate<ResourceManager>("ResourceManager");
        yield return null; // 한 프레임 대기

        string applicationName = Application.productName.Replace(" : ", "-").Replace(" ", "_");
        // 2_1 시스템 데이터 등록
        Task<IOResult> applyGameIdentityTask = ContentManagementSystem.ApplyGameIdentityAsync(defaultManifestPath + applicationName, this.GetType());

        while (applyGameIdentityTask.IsCompleted == false)
        {
            yield return null;
        }

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

        if (applyGameIdentityTask.IsFaulted || !applyGameIdentityTask.Result.succeed)
        {
            // 유저에게 업데이트를 수행할 것을 요구해야함
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<ContentSyncContext> syncTask = ContentManagementSystem.SyncContentAsync(
                ResourceManager.ManifestStaticKey, cts.Token);

            while (!syncTask.IsCompleted)
            {
                yield return null;
            }

            string message = null;
            string reason = syncTask.IsFaulted ? syncTask.Exception?.GetBaseException().Message : syncTask.Result.failReason.ToString();    

            if (syncTask.IsFaulted)
            {
                message = $"필수 구성 요소 업데이트 중 오류가 발생했습니다.\n앱을 종료합니다.\n 사유 : {reason}.";
         
                Notifier.NotifyError(
                    "CriticalError",
                    message,
                    NotifyChannel.Native,
                    () =>
                    {
    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
    #else
                Application.Quit();
    #endif
                    }
                );

                yield break;
            }
        }
#if UNITY_EDITOR
        Debug.Log("[BootStrap] All Managers Initialized.");
#endif
    }
   private T PromoteOrCreate<T>(string goName) where T : Component
    {
        T existing = FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return existing;
        }

        GameObject go = new GameObject(goName);
        DontDestroyOnLoad(go);
        return go.AddComponent<T>();
    }
 
#endregion Custom Methods
}
