using Unity.Netcode;
using UnityEngine;
using UnityEngine.Android;

/// <summary>
/// 게임 매니저 싱글톤 클래스
/// </summary>
public class GameManager : NetworkBehaviour
{
    public enum GameDifficulty
    {
        Normal,
        Hard
    }

    public static GameManager instance;

    [SerializeField]
    private GameDifficulty difficulty;

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
        int randomValue = Random.Range(0, 2);

        if (randomValue == 0)
        {
            difficulty = GameDifficulty.Normal;
        }
        else
        {
            difficulty = GameDifficulty.Hard;
        }
    }
    #endregion Unity Methods

    #region Custom Methods
    public GameDifficulty GetGameMode()
    {
        return difficulty;
    }
    #endregion
}
