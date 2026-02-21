using Assets.Scripts.Content;
using Assets.Scripts.Interface;
using System.Collections;
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

    /// <summary>
    /// 외부에서 호출하는 진입점.
    /// 실제 코루틴 실행은 내부 인스턴스가 담당.
    /// </summary>
    public static void LoadScene(uint sceneStaticKey)
    {
        if (instance == null)
        {
            Debug.LogWarning("[SceneLoadManager] instance is null.");
            return;
        }

        instance.StartCoroutine(instance.C_LoadScene(sceneStaticKey));
    }
 
    private IEnumerator C_LoadScene(uint sceneStaticKey)
    {
        if (ResourceManager.instance == null)
        {
            Debug.LogError("[SceneLoadManager] ResourceManager is null.");
            yield break;
            //yield return StartCoroutine(ResourceManager.instance.C_PrepareScene(sceneStaticKey));
        }

        if (!ResourceManager.instance.TryGetContentEntry(sceneStaticKey, out ContentEntry entry))
        {
            Debug.LogError($"[SceneLoadManager] Scene entry not found. staticKey=0x{sceneStaticKey:X8}");
            yield break;
        }

        if (entry is not SceneEntry sceneEntry)
        {
            Debug.LogError($"[SceneLoadManager] Entry is not SceneEntry. staticKey=0x{sceneStaticKey:X8}, type={entry.GetType().Name}");
            yield break;
        }

        if (string.IsNullOrEmpty(sceneEntry.header.id))
        {
            Debug.LogError($"[SceneLoadManager] Scene name is empty. staticKey=0x{sceneStaticKey:X8}");
            yield break;
        }

        currentSceneKey = sceneStaticKey;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneEntry.header.id, LoadSceneMode.Single);
        if (op == null)
        {
            Debug.LogError($"[SceneLoadManager] LoadSceneAsync failed. sceneName={sceneEntry.header.id}");
            yield break;
        }

        while (!op.isDone)
        {
            yield return null;
        }
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