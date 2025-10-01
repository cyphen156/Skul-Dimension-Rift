using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static Types;

/// <summary>
/// 사용자의 입력을 관리하는 싱글톤 매니저 클래스입니다.
/// 게임매니저에 의해 제어되며 
/// 사용자 입력은 항상 어떠한 방식으로든 들어오는 이벤트로 간주합니다.
/// 따라서, 이 매니저는 입력 이벤트를 수신하고 이를 처리하는 역할을 합니다.
/// </summary>
public class InputManager : NetworkBehaviour
{
    public static InputManager instance;

    [SerializeField] private InputMode currentInputMode;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private InputActionMap currentActionMap;
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

        playerInput = GetComponent<PlayerInput>();
        ChangeInputMode(InputMode.Locked);
    }
    #endregion Unity Methods

    #region Custom Methods
    /// <summary>
    /// Input ActionMap을 전환합니다.
    /// </summary>
    /// <param name="mapName"></param>
    private void ChangeActionMap(string mapName)
    {
        // 모든 액션맵을 비활성화 한 뒤 특정 액션맵만 활성화
        playerInput.actions.Disable();
        playerInput.SwitchCurrentActionMap(mapName);
        playerInput.currentActionMap.Enable();
        playerInput.ActivateInput();
        currentActionMap = playerInput.currentActionMap;
    }

    /// <summary>
    /// InputMode를 ActionMap을 전환하여 입력 모드를 변경합니다.
    /// 상태 제어는 딱히 필요하지 않지만, 
    /// 필요시 여기에 추가할 수 있습니다.
    /// </summary>
    /// <param name="mode">변경할 모드</param>
    public void ChangeInputMode(InputMode mode)
    {
        currentInputMode = mode;

        switch (currentInputMode)
        {
            case InputMode.Locked:
                // 입력 잠금 상태에서의 처리
                {
                    ChangeActionMap("UI");
                    playerInput.DeactivateInput();
                }
                break;
            case InputMode.UIOnly:
                // UI 전용 입력 처리
                {
                    ChangeActionMap("UI");
                }
                break;
            case InputMode.PlayerOnly:
                // 플레이어 전용 입력 처리
                {
                    ChangeActionMap("Player");
                }
                break;
            case InputMode.Ready:
                // 준비 상태에서의 처리
                {
                    ChangeActionMap("Title");
                }
                break;
            case InputMode.Restricted:
                // 제한된 플레이어 입력 처리
                // (예: 대화 중, 컷신 등)
                {
                    ChangeActionMap("Player");
                    // 상호작용과 시스템 메뉴 호출만 활성화
                    playerInput.actions.Disable();
                    playerInput.currentActionMap["Interaction"].Enable();
                    playerInput.currentActionMap["Menu"].Enable();
                    playerInput.ActivateInput();
                }
                break;
            default:
                break;
        }
#if UNITY_EDITOR
        Debug.LogWarning("Current InputMode: " + currentInputMode);
        Debug.LogWarning("Current ActionMap: " + playerInput.currentActionMap.name);
#endif
    }
    public void OnPressed(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            Debug.Log("Pressed: " + ctx.action.name);
            GameManager.instance.ChangeGameState(GameState.Playing);
        }
    }
#endregion Custom Methods
}
