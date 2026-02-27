using Assets.Scripts.Content;
using Assets.Scripts.Interface;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬을 로드하고 종속성 컨텐츠를 처리하는 매니저.
/// </summary>
public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager instance;

    private uint currentSceneKey;

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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public IEnumerator C_LoadScene(uint sceneStaticKey)
    {
        ResourceManager rm = ResourceManager.instance;

        if (rm == null)
        {
            Debug.LogError("[SceneLoadManager] ResourceManager is null.");
            yield break;
        }

        if (!rm.TryGetContentEntry(sceneStaticKey, out ContentEntry e) || e == null)
        {
            Debug.LogError($"[SceneLoadManager] ContentEntry not found. key={sceneStaticKey}");
            yield break;
        }

        SceneEntry sceneEntry = e as SceneEntry;

        if (sceneEntry == null)
        {
            Debug.LogError($"[SceneLoadManager] Entry is not SceneEntry. key={sceneStaticKey}, type={e.GetType().Name}");
            yield break;
        }

        CancellationTokenSource cts = new CancellationTokenSource();
        Task<IOResult> prepareTask = ContentManagementSystem.PrepareContentAsync(sceneStaticKey, cts.Token);

        while (!prepareTask.IsCompleted)
        {
            if (cts.IsCancellationRequested)
            {
                cts.Dispose();
                yield break;
            }
            //UIManager.instance.RefreshUI("LoadingScreen", sceneStaticKey.ToString());
            yield return null;
        }

        if (prepareTask.IsFaulted)
        {
            Debug.LogException(prepareTask.Exception);
            cts.Dispose();
            yield break;
        }

        IOResult r = prepareTask.Result;

        if (r == null || !r.succeed)
        {
            Debug.LogError($"[SceneLoadManager] Prepare failed. key={sceneStaticKey}, reason={(r == null ? "null" : r.failReason.ToString())}");
            cts.Dispose();
            yield break;
        }

        // 불필요한 자원 해제 요청


        currentSceneKey = sceneStaticKey;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneEntry.header.id, LoadSceneMode.Single);

        if (op == null)
        {
            Debug.LogError($"[SceneLoadManager] LoadSceneAsync failed. sceneName={sceneEntry.header.id}");
            cts.Dispose();
            yield break;
        }

        while (!op.isDone)
        {
            if (cts.IsCancellationRequested)
            {
                cts.Dispose();
                yield break;
            }
            yield return null;
        }
        cts.Dispose();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (StageManager.instance == null)
        {
            Debug.LogError("[CriticalError] StageManager is null.");
            return;
        }

        StageManager.instance.OnSceneLoaded(scene.name, currentSceneKey);

        if (scene.name == "TitleScene")
        {
            IPlayable intro = GameObject.Find("Intro").GetComponent<IPlayable>();
            StartCoroutine(intro.C_Play());
            return;
        }
        
        GameManager.instance.ChangeGameState(Types.GameState.Ready);
    }
}