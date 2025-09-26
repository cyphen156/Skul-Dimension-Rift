using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Types;

/// <summary>
/// 게임 매니저 싱글톤 클래스
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    [SerializeField]
    private GameDifficulty difficulty;
    private GameState currentState; 
    private string currentSceneName;

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

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        Scene scene = SceneManager.GetActiveScene();
        currentSceneName = scene.name;
        if (currentSceneName != "TitleScene")
        {
            SceneLoadManager.LoadScene("TitleScene");
        }

        ResetGame();
    }
    #endregion Unity Methods

    #region Custom Methods

    public GameDifficulty GetGameMode()
    {
        return difficulty;
    }

    private bool ResetGame()
    {
        ChangeGameState(GameState.None);
        // 난이도 설정 (임시로 랜덤)
        int randomValue = Random.Range(0, 2);

        if (randomValue == 0)
        {
            difficulty = GameDifficulty.Default;
        }
        else
        {
            difficulty = GameDifficulty.Hard;
        }

        return true;
    }

    public void ChangeGameState(GameState state)
    {
        if (currentState == state)
        {
            return;
        }

        switch(state)
        {
            case GameState.Ready:
                if (currentSceneName == "TitleScene")
                {
                    UIManager.instance.Show("Press Any Key");
                    //InputManager.instance.EnablePlayerInput();
                }
                break;
            case GameState.Playing:
                UIManager.instance.Show("InGameUI");
                break;
            case GameState.Paused:
                UIManager.instance.Show("PauseUI");
                break;
            case GameState.GameOver:
                UIManager.instance.Show("GameOverUI");
                break;
            case GameState.Victory:
                UIManager.instance.Show("VictoryUI");
                break;
            default:
                break;
        }
    }


    #endregion
}
