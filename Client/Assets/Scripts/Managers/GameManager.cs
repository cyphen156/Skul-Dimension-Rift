using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using WebSocketSharp;
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
    private Dictionary<ulong, PlayerController> connectedPlayers = new Dictionary<ulong, PlayerController>();

    [Header("GameState")]
    [SerializeField] private GameDifficulty difficulty;
    [SerializeField] private GameState currentState;

    [Header("Players")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private NetworkObject localPlayer;

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
        playerPrefab = ResourceManager.instance.GetGameObject("Player");

        ResetGame();
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

    public GameDifficulty GetGameDifficulty()
    {
        return difficulty;
    }

    public GameMode GetGameMode()
    {
        return gameMode;
    }

    public GameState GetGameState()
    {
        return currentState;
    }

    private bool ResetGame()
    {
        ChangeGameState(GameState.Reset);
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
                UIManager.instance.Hide("Loading");
                if (StageManager.instance.GetCurrentSceneName() == "TitleScene")
                {
                    UIManager.instance.Show("Press Any Key");
                    InputManager.instance.ChangeInputMode(InputMode.Ready);
                }
                break;
            case GameState.Playing:
                UIManager.instance.HideAll();
                InputManager.instance.ChangeInputMode(InputMode.PlayerOnly);
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
            case GameState.Reset:
                InputManager.instance.ChangeInputMode(InputMode.Locked);
                UIManager.instance.HideAll();
                break;
            case GameState.None:
                break;
            default:
                // 정의되지 않은 상태
                // 이 상태는 들어와선 안됨
                break;
        }
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ControlReBind(string buttonName = null)
    {
        // 만약 buttonName이 null이면
        // 전체 리셋
        if (buttonName == null)
        {
            InputManager.instance.ResetBindings();
            UIManager.instance.RefreshUI("Control");
            ResourceManager.instance.SaveUserData();
            return;
        }

        StartCoroutine(C_GetKeyInfo(buttonName));
    }

    private IEnumerator C_GetKeyInfo(string buttonName)
    {
        GameObject popUpUI = UIManager.instance.TryGetUI("PopUp");

        if (popUpUI == null)
        {
            Debug.LogWarning("PopUp UI not found.");
            yield break;
        }

        // 키 변경 팝업 호출
        UIManager.instance.Show("PopUp");
        InputManager.instance.ChangeInputMode(InputMode.Restricted, "ControlRebind");
        while (popUpUI.activeInHierarchy)
        {
            // 동프레임 입력누적을 처리하기 위한 한프레임 대기
            yield return null;
            // 키 하나 입력받을 때까지 대기
            yield return new WaitUntil(() =>
                !popUpUI.activeInHierarchy || InputManager.instance.GetAnyKey()
            );

            yield return null;

            if (!popUpUI.activeInHierarchy)
            {
                break;
            }

            var ctx = InputManager.instance.PeekLastInput();
            UIManager.instance.Execute(ctx);
            InputManager.instance.EndCapture();
        }

        InputManager.instance.EndCapture();
        InputManager.instance.ChangeInputMode(InputMode.UIOnly);

        // 팝업 UI에서 입력받은 키 정보 가져오기
        PopUp popUpComponent = popUpUI.GetComponent<PopUp>();

        // Execute_Internal에 의한 PopUp Close
        if (!popUpComponent.CheckConfirm())
        {
            yield break;
        }

        InputControl newKey = popUpComponent.GetBindingControl();
        string switchButton;
        InputManager.instance.RebindAction(buttonName, out switchButton, newKey);
        yield return null;
        // 리바인드 종료 로직
        UIManager.instance.RefreshUI("Control", buttonName);
        // 
        if (switchButton != null)
        {
            UIManager.instance.RefreshUI("Control", switchButton);
        }
        InputManager.instance.ChangeInputMode(InputMode.UIOnly);
        ResourceManager.instance.SaveUserData();
    }

    public void ApplyUserOptionSetting(UIWidgetContainer widget)
    {
        switch (widget.groupKey)
        {
            case OptionDataType.Graphic:
                GraphicManager.instance.ApplyOption(widget);
                break;
            case OptionDataType.Data:
                ResourceManager.instance.ApplyOption(widget);
                break;
            case OptionDataType.Audio:
                SoundManager.instance.ApplyOption(widget);
                break;
            case OptionDataType.GamePlay:
                ApplyOption(widget);
                break;
            default:
                break;
        }
    }

    private void ApplyOption(UIWidgetContainer widget)
    {
        // 아직 아무 행동도 하지 않습니다.
        GamePlayDataType type;
        Types.gamePlayDataType.TryGetValue(widget.parentName, out type);
        switch (type)
        {
            case GamePlayDataType.Languages:
                break;
            case GamePlayDataType.RukiMode:
                break;
            case GamePlayDataType.ShowTimer:
                break;
            case GamePlayDataType.ShowUIs:
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 옵션 적용 결과를 다시 UI에 전달하는 외부 공개 API
    /// </summary>
    /// <param name="targetUI"></param>
    /// <param name="data"></param>
    public void ReplyOptionResult(string targetUI = null, string data = null)
    {
        UIManager.instance.RefreshUI(targetUI, data);
    }

    /// <summary>
    /// 유저 데이터를 저장하기 위한 외부 공개 API
    /// 추후 상황에 따른 권한과 실행 제어 설정 필요
    /// </summary>
    public void SaveUserData()
    {
        GraphicManager.instance.ApplyResolutionSetting();
        ResourceManager.instance.SaveUserData();
    }
    #endregion

    public void RequestChangeScene(uint sceneKey)
    {
        // 싱글 플레이 모드
        if (gameMode == GameMode.Single)
        {
            ChangeGameState(GameState.Loading);
            SceneLoadManager.LoadScene(sceneKey);
            return;
        }

        // 멀티 플레이 모드
        if (IsServer == true)
        {
            ChangeGameState(GameState.Loading);
            SceneLoadManager.LoadScene(sceneKey);
        }
        else
        {
            RequestChangeSceneServerRpc(sceneKey);
        }
    }


    #region MultiPlay
    public void EnsureLocalPlayer(Vector3 spawnPosition)
    {
        if (localPlayer != null && localPlayer.IsSpawned)
        {
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[GameManager] NetworkManager is null.");
            return;
        }

        if (IsServer)
        {
            SpawnPlayerInternal(NetworkManager.Singleton.LocalClientId, spawnPosition);
        }
        else
        {
            // 클라이언트라면 서버에게 스폰 요청
            SpawnPlayerServerRpc(spawnPosition);
        }
    }

    private void SpawnPlayerInternal(ulong clientId, Vector3 spawnPosition)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[GameManager] playerPrefab is null.");
            return;
        }

        GameObject instance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

        NetworkObject netObj = instance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[GameManager] Player prefab has no NetworkObject.");
            Destroy(instance);
            return;
        }

        netObj.SpawnAsPlayerObject(clientId, false); 

        PlayerController controller = instance.GetComponent<PlayerController>();
        if (controller != null)
        {
            if (!connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers.Add(clientId, controller);
            }
            else
            {
                connectedPlayers[clientId] = controller;
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlayerServerRpc(Vector3 spawnPosition)
    {
        SpawnPlayerInternal(OwnerClientId, spawnPosition);
    }

    [Rpc(SendTo.Server)]
    private void RequestChangeSceneServerRpc(uint sceneKey)
    {
        ChangeGameState(GameState.Loading);
        SceneLoadManager.LoadScene(sceneKey);
    }
#endregion
}