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
        if (ResourceManager.instance != null)
        {
            yield return StartCoroutine(ResourceManager.instance.C_PrepareScene(sceneStaticKey));
        }

        currentSceneKey = sceneStaticKey;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (StageManager.instance == null)
        {
            Debug.LogError("[CriticalError] StageManager is null.");
            return;
        }

        StageManager.instance.OnSceneLoaded(scene.name, currentSceneKey);

        GameManager.instance.ChangeGameState(Types.GameState.Ready);
        if (scene.name == "TitleScene")
        {
            IPlayable intro = GameObject.Find("Intro").GetComponent<IPlayable>();
            StartCoroutine(intro.C_Play());
        }
    }
}