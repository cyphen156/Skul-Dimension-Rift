using Assets.Scripts.Interface;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager instance;

    private Dictionary<string, uint> scenes = new Dictionary<string, uint>();

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
        scenes.Clear();

        // 기본 씬 매핑
        scenes["TitleScene"] = 0xFFFF0000u;
        scenes["Stage0Scene"] = 0xFFFF1000u;
        scenes["Stage1Scene"] = 0xFFFF2000u;
        scenes["Stage2Scene"] = 0xFFFF3000u;
    }

    public void RegisterScene(uint id, string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) == true)
        {
            return;
        }

        if (scenes.ContainsKey(sceneName) == true)
        {
            return;
        }

        scenes[sceneName] = id;
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
        uint sceneId;
        bool hasScene = scenes.TryGetValue(sceneName, out sceneId);

        if (hasScene == false)
        {
            Debug.LogWarning("[SceneLoadManager] SceneId not found : " + sceneName);
            yield break;
        }

        if (ResourceManager.instance != null)
        {
            yield return StartCoroutine(ResourceManager.instance.C_LoadSceneData(sceneId));
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        while (op.isDone == false)
        {
            yield return null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameManager.instance.SetCurrentScene(scene.name);

        if (StageManager.instance != null)
        {
            StageManager.instance.SetSceneName(scene.name);
        }

        switch (scene.name)
        {
            case "TitleScene":
                {
                    Intro intro = FindFirstObjectByType<Intro>();

                    if (intro != null && intro is IPlayable)
                    {
                        StartCoroutine(((IPlayable)intro).C_Play());
                    }
                    break;
                }

            default:
                {
                    break;
                }
        }

        GameManager.instance.ChangeGameState(Types.GameState.Ready);
    }
}
