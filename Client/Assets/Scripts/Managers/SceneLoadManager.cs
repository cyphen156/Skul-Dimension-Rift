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
    /// �� �ε� �� 
    /// ��巹���� �ּ��� �ε� ������ ����
    /// ���Ŵ����� ���� �Լ�
    /// </summary>
    /// <param name="sceneName"></param>
    public static void LoadScene(string sceneName)
    {
        // ��巹���� �ּ� �ε�


        // �ּ� �ε� �Ϸ� �� �� �ε�
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// �� �ε� �Ϸ� �� ȣ��Ǵ� �ݹ� �޼���
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
