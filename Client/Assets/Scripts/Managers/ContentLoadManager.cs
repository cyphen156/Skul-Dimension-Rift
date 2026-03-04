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
public class ContentLoadManager : MonoBehaviour
{
    public static ContentLoadManager instance;

    public enum ContentLoadSignal
    {
        Alive,
        Complete,
        Failed
    }

    public delegate void ContentLoadSignalHandler(int transitionId, ContentLoadSignal signal);
    public event ContentLoadSignalHandler onContentLoadSignal;

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

    // 로컬 호출
    public IEnumerator C_LoadContent(uint sceneStaticKey)
    {
        yield return C_LoadContent_Internal(sceneStaticKey, -1, 0f);
    }

    // 멀티 호출
    public IEnumerator C_LoadContent(uint sceneStaticKey, int transitionId, float purseInterval)
    {
        yield return C_LoadContent_Internal(sceneStaticKey, transitionId, purseInterval);
    }

    private IEnumerator C_LoadContent_Internal(uint sceneStaticKey, int transitionId, float purseInterval)
    {
        ResourceManager rm = ResourceManager.instance;

        bool pulseEnabled = false;
        if (transitionId >= 0 && purseInterval > 0f)
        {
            pulseEnabled = true;
        }

        if (rm == null)
        {
            Debug.LogError("[SceneLoadManager] ResourceManager is null.");
            RaiseSignal(transitionId, sceneStaticKey, ContentLoadSignal.Failed);
            yield break;
        }

        ContentEntry e;
        if (!rm.TryGetContentEntry(sceneStaticKey, out e) || e == null)
        {
            Debug.LogError($"[SceneLoadManager] ContentEntry not found. key={sceneStaticKey}");
            RaiseSignal(transitionId, sceneStaticKey, ContentLoadSignal.Failed);
            yield break;
        }

        SceneEntry sceneEntry = e as SceneEntry;

        if (sceneEntry == null)
        {
            Debug.LogError($"[SceneLoadManager] Entry is not SceneEntry. key={sceneStaticKey}, type={e.GetType().Name}");
            RaiseSignal(transitionId, sceneStaticKey, ContentLoadSignal.Failed);
            yield break;
        }

        CancellationTokenSource cts = new CancellationTokenSource();
        Task<IOResult> prepareTask = ContentManagementSystem.PrepareContentAsync(sceneStaticKey, cts.Token);

        float nextPulseTime = 0f;
        if (pulseEnabled)
        {
            RaiseSignal(transitionId, sceneStaticKey, ContentLoadSignal.Alive);
            nextPulseTime = Time.realtimeSinceStartup + purseInterval;
        }

        while (!prepareTask.IsCompleted)
        {
            if (cts.IsCancellationRequested)
            {
                cts.Dispose();
                RaiseSignal(transitionId, sceneStaticKey, ContentLoadSignal.Failed);
                yield break;
            }

            if (pulseEnabled)
            {
                if (Time.realtimeSinceStartup >= nextPulseTime)
                {
                    RaiseSignal(transitionId, sceneStaticKey, ContentLoadSignal.Alive);
                    nextPulseTime = Time.realtimeSinceStartup + purseInterval;
                }
            }

            yield return null;
        }

        if (prepareTask.IsFaulted)
        {
            Debug.LogException(prepareTask.Exception);
            cts.Dispose();
            RaiseSignal(transitionId, sceneStaticKey, ContentLoadSignal.Failed);
            yield break;
        }

        IOResult r = prepareTask.Result;

        if (r == null || !r.succeed)
        {
            Debug.LogError($"[SceneLoadManager] Prepare failed. key={sceneStaticKey}, reason={(r == null ? "null" : r.failReason.ToString())}");
            cts.Dispose();
            RaiseSignal(transitionId, sceneStaticKey, ContentLoadSignal.Failed);
            yield break;
        }

        currentSceneKey = sceneStaticKey;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneEntry.header.id, LoadSceneMode.Single);

        if (op == null)
        {
            Debug.LogError($"[SceneLoadManager] LoadSceneAsync failed. sceneName={sceneEntry.header.id}");
            cts.Dispose();
            RaiseSignal(transitionId, sceneStaticKey, ContentLoadSignal.Failed);
            yield break;
        }

        if (pulseEnabled)
        {
            nextPulseTime = Time.realtimeSinceStartup + purseInterval;
        }

        while (!op.isDone)
        {
            if (cts.IsCancellationRequested)
            {
                cts.Dispose();
                RaiseSignal(transitionId, sceneStaticKey, ContentLoadSignal.Failed);
                yield break;
            }

            if (pulseEnabled)
            {
                if (Time.realtimeSinceStartup >= nextPulseTime)
                {
                    RaiseSignal(transitionId, sceneStaticKey, ContentLoadSignal.Alive);
                    nextPulseTime = Time.realtimeSinceStartup + purseInterval;
                }
            }

            yield return null;
        }

        cts.Dispose();
        RaiseSignal(transitionId, sceneStaticKey, ContentLoadSignal.Complete);
    }

    private void RaiseSignal(int transitionId, uint sceneStaticKey, ContentLoadSignal signal)
    {
        if (transitionId < 0)
        {
            return;
        }

        ContentLoadSignalHandler handler = onContentLoadSignal;
        if (handler == null)
        {
            return;
        }

        handler(transitionId, signal);
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