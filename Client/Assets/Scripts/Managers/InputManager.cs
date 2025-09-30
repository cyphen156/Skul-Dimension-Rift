using System;
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

    private InputMode currentInputMode;
    private PlayerInput playerInput;

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
                playerInput.DeactivateInput();
                break;
            case InputMode.UIOnly:
                // UI 전용 입력 처리
                {
                    playerInput.SwitchCurrentActionMap("UI");
                    playerInput.ActivateInput();
                }
                break;
            case InputMode.PlayerOnly:
                // 플레이어 전용 입력 처리
                {
                    playerInput.SwitchCurrentActionMap("Player");
                    playerInput.ActivateInput();
                }
                break;
            default:
                break;
        }
    }
    #endregion Custom Methods
}
