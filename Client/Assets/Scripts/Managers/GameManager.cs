using UnityEngine;

/// <summary>
/// ���� �Ŵ��� �̱��� Ŭ����
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

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
    }
}
