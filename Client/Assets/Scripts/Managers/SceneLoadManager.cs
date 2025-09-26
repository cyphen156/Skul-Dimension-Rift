using Assets.Scripts.Interface;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager instance;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 씬 로드 전 
    /// 어드레서블 애셋의 로드 보장을 위한
    /// 씬매니저의 래퍼 함수
    /// </summary>
    /// <param name="sceneName"></param>
    public static void LoadScene(string sceneName)
    {
        // 어드레서블 애셋 로드


        // 애셋 로드 완료 후 씬 로드
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 씬 로드 완료 후 호출되는 콜백 메서드
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mode"></param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "TitleScene":
                Intro intro = FindFirstObjectByType<Intro>();
                if (intro != null && intro is IPlayable)
                {
                    StartCoroutine(((IPlayable)intro).C_Play());
                }
                break;
            default:
                break;
        }
    }
}
