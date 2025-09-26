using UnityEngine;

/// <summary>
/// ��巹���� ���ҽ� ������ ���� �Ŵ��� Ŭ����
/// �� ������ �ε�Ǵ� ���ҽ����� ����
/// �� ���� ���ҽ��� �� ������ ����
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager instance;

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
        }

        Initialize();
    }
    #endregion Unity Methods


    #region Custom Methods
    private void Initialize()
    {

    }
    #endregion
}
