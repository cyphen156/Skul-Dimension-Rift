using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

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
    }
    #endregion Unity Methods    

    #region Custom Methods
    public void PlayBGM(string bgmName)
    {
        Debug.Log($"Playing BGM: {bgmName}");
    }
    #endregion Custom Methods
}
