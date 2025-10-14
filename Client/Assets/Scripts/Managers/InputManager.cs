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
    }

    private void OnDisable()
    {
        // 모든 액션맵 비활성화
        if (playerInput != null && playerInput.actions != null)
        {
            playerInput.actions.Disable();
        }
    }
    private void Start()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }
        AllocateInputActions();
        ChangeInputMode(InputMode.Locked);
    }

    #endregion Unity Methods

    #region Custom Methods
    /// <summary>
    /// 인풋 액션을 각 맵에 할당합니다.
    /// 중복할당 문제를 해결하기 전까지 사용하지 마세요 (2025-10-14)
    /// 인스펙터로 할당하는 방식을 권장합니다.
    /// </summary>
    private void AllocateInputActions()
    {
        foreach (var map in playerInput.actions.actionMaps)
        {
            switch(map.name)
            {
                case "Player":
                    {
                        PlayerController playerController = GetComponent<PlayerController>();

                        // 플레이어 컨트롤러가 존재하는 경우에만 액션을 할당
                        // 액션에 해당하는 행위를 리플렉션으로 연결
                        if (playerController != null)
                        {
                            foreach (var action in map.actions)
                            {
                                string actionName = action.name;
                                // Menu와 Scroll은 UIManager에 연결
                                if (actionName == "Menu" || actionName == "Scroll")
                                {
                                    action.performed -= UIManager.instance.Execute_Internal;
                                    action.performed += UIManager.instance.Execute_Internal;
                                }
                                else
                                {
                                    var method = playerController.GetType().GetMethod("On" + actionName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                    if (method != null)
                                    {
                                        action.performed -= (InputAction.CallbackContext ctx) => method.Invoke(playerController, new object[] { ctx });
                                        action.performed += (InputAction.CallbackContext ctx) => method.Invoke(playerController, new object[] { ctx });
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"InputManager: Method 'On{actionName}' not found in PlayerController.");
                                    }
                                }
                            }
                        }
                    }
                    break;
                case "UI":
                    {
                        foreach (var action in map.actions)
                        {
                            string actionName = action.name;
                            if (actionName == "Menu" || actionName == "Scroll")
                            {
                                action.performed -= UIManager.instance.Execute_Internal;
                                action.performed += UIManager.instance.Execute_Internal;
                            }
                            else
                            {
                                action.performed -= UIManager.instance.Execute;
                                action.performed += UIManager.instance.Execute;
                            }
                        }
                    }
                    break;
                case "Title":
                    {
                        PressAnyKey pressAnyKey = GetComponent<PressAnyKey>();
                        if (pressAnyKey != null)
                        {
                            map["PressAnyKey"].performed -= pressAnyKey.Execute;
                            map["PressAnyKey"].performed += pressAnyKey.Execute;
                        }
                    }
                    break;
                case "Locked":
                    {
                        // Locked 맵에는 특별한 액션이 없으므로 아무 것도 하지 않음
                    }
                    break;
                default:
                    {
                        Debug.LogWarning($"InputManager: Unhandled ActionMap '{map.name}'");
                    }
                    break;
            }
            Debug.Log($"[ActionMap] {map.name} has been allocated.");
        }
    }

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
                    ChangeActionMap("Locked");
                    UIManager.instance.HideAll();
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
#endregion Custom Methods
}
