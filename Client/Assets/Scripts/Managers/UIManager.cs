using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] 
    
    public static UIManager instance;
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

    public void Show(string UIName)
    {
        Debug.Log(UIName);
    }
}
