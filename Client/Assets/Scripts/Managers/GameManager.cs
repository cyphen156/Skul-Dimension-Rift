using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Types;

/// <summary>
/// 게임 매니저 싱글톤 클래스
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    [Header("NetworkSettings")]
    [SerializeField] private GameMode gameMode;
    [SerializeField] private bool isCoopMode;

    [Header("GameState")]
    [SerializeField] private string currentSceneName;
    [SerializeField] private GameDifficulty difficulty;
    [SerializeField] private GameState currentState;


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
        ResetGame();
        Scene scene = SceneManager.GetActiveScene();
        currentSceneName = scene.name;
        if (currentSceneName != "TitleScene")
        {
            SceneLoadManager.LoadScene("TitleScene");
        }
    }

    public override void OnNetworkSpawn()
    {
        if (isCoopMode)
        {
            ChangeGameMode(GameMode.MultiplayerCoop);
        }
        else
        {
            ChangeGameMode(GameMode.MultiplayerVersus);
        }
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        ChangeGameMode(GameMode.Single);
        base.OnNetworkDespawn();
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
        ChangeGameMode(GameMode.Single);
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

    private void ChangeGameMode(GameMode mode)
    {
        gameMode = mode;
        switch (mode)
        {
            case GameMode.Single:
                if (IsServer)
                {
                    NetworkManager.Singleton.Shutdown();
                }
                break;
            case GameMode.MultiplayerCoop:
                // 멀티플레이어 모드 진입
                if (!IsServer)
                {
                    NetworkManager.Singleton.StartClient();
                }
                break;
            case GameMode.MultiplayerVersus:
                // 추후 구현 예정
                // 멀티플레이어 모드 진입
                if (!IsServer)
                {
                    NetworkManager.Singleton.StartClient();
                }
                break;
            default:
                break;
        }
    }
    public void ChangeGameState(GameState state)
    {
        if (currentState == state)
        {
            return;
        }

        currentState = state;

        switch (state)
        {
            case GameState.Ready:
                if (currentSceneName == "TitleScene")
                {
                    UIManager.instance.Show("Press Any Key");
                    InputManager.instance.ChangeInputMode(InputMode.Ready);
                }
                break;
            case GameState.Playing:
                if (currentSceneName == "TitleScene")
                {
                    //SceneLoadManager.LoadScene("MainScene");
                }
                //InputManager.instance.ChangeInputMode(InputMode.PlayerOnly);
                InputManager.instance.ChangeInputMode(InputMode.UIOnly);
                break;
            case GameState.Paused:
                InputManager.instance.ChangeInputMode(InputMode.UIOnly);
                break;
            case GameState.GameOver:
                InputManager.instance.ChangeInputMode(InputMode.Restricted);
                break;
            case GameState.Victory:
                InputManager.instance.ChangeInputMode(InputMode.Restricted);
                break;
            case GameState.Loading:
                // 로딩중일 땐 입력을 막아주고 로딩 패널 재생
                InputManager.instance.ChangeInputMode(InputMode.Locked);
                UIManager.instance.HideAll();
                UIManager.instance.Show("Loading");
                break;
            case GameState.None:
                InputManager.instance.ChangeInputMode(InputMode.Locked);
                UIManager.instance.HideAll();
                break;
            default:
                // 정의되지 않은 상태
                // 이 상태는 들어와선 안됨
                break;
        }
    }


    #endregion
}
