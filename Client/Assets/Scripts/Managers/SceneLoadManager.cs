using Assets.Scripts.Content;
using Assets.Scripts.Data;
using Assets.Scripts.Interface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬을 로드하고 종속성 컨텐츠를 처리하는 매니저.
/// </summary>
public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager instance;

    private Dictionary<string, uint> sceneMap = new Dictionary<string, uint>();

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

        Initialize();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Initialize()
    {
        sceneMap.Clear();

        IReadOnlyList<SceneEntry> scenes = ResourceManager.instance.GetSceneEntries();

        foreach (SceneEntry scene in scenes)
        {
            RegisterScene(scene);
        }
    }

    public void RegisterScene(SceneEntry scene)
    {
        if (scene.sceneName == string.Empty || scene.staticKey == string.Empty)
        {
            return;
        }

        if (DomainKeyParser.TryParseStaticKey(scene.staticKey, out uint staticKey))
        {
            sceneMap.Add(scene.sceneName, staticKey);
        }
    }

    /// <summary>
    /// 외부에서 호출하는 진입점.
    /// 실제 코루틴 실행은 내부 인스턴스가 담당.
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        if (instance == null)
        {
            Debug.LogWarning("[SceneLoadManager] instance is null.");
            return;
        }

        instance.StartCoroutine(instance.C_LoadScene(sceneName));
    }

    private IEnumerator C_LoadScene(string sceneName)
    {
        uint sceneStaticId;

        if (sceneMap.TryGetValue(sceneName, out sceneStaticId) == false)
        {
            Debug.LogWarning("[SceneLoadManager] SceneId not found : " + sceneName);
            yield break;
        }

        if (ResourceManager.instance != null)
        {
            yield return StartCoroutine(ResourceManager.instance.C_LoadSceneData(sceneStaticId));
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        while (op.isDone == false)
        {
            yield return null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        uint sceneStaticId;

        if (sceneMap.TryGetValue(scene.name, out sceneStaticId) == false)
        {
            sceneStaticId = 0u;
        }

        if (StageManager.instance == null)
        {
            Debug.LogError("[CriticalError] StageManager is null.");
            return;
        }

        StageManager.instance.OnSceneLoaded(scene.name, sceneStaticId);

        GameManager.instance.ChangeGameState(Types.GameState.Ready);
        if (scene.name == "TitleScene")
        {
            IPlayable intro = GameObject.Find("Intro").GetComponent<IPlayable>();
            StartCoroutine(intro.C_Play());
        }
    }
}