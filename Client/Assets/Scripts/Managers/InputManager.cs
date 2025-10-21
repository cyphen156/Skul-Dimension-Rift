using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
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

    [SerializeField] private static InputAction captureAction;
    [SerializeField] private static InputAction.CallbackContext lastInputContext;
    [SerializeField] private static bool isContextReady;

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

        if (captureAction != null)
        {
            captureAction.performed -= OnCapturePerformedAction;
            if (captureAction.enabled)
            {
                captureAction.Disable();
            }
        }

        isContextReady = false;
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
    private static void InitializeCaptureAction()
    {
        if (captureAction == null)
        {
            captureAction = new InputAction("AnyKeyCapture", InputActionType.Button);
        }

        captureAction.performed -= OnCapturePerformedAction;
        captureAction.performed += OnCapturePerformedAction;

        if (captureAction.enabled)
        {
            captureAction.Disable();
        }
    }
    /// <summary>
    /// 인풋 액션을 각 맵에 할당합니다.
    /// 중복할당 문제를 해결하기 전까지 사용하지 마세요 (2025-10-14)
    /// 인스펙터로 할당하는 방식을 권장합니다.
    /// </summary>
    private void AllocateInputActions()
    {
        foreach (var map in playerInput.actions.actionMaps)
        {
            bool hasAllocated = true;
            switch (map.name)
            {
                case "Player":
                    {
                        PlayerController playerController = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);

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
                        PressAnyKey pressAnyKey = FindAnyObjectByType<PressAnyKey>(FindObjectsInactive.Include);

                        if (pressAnyKey == null)
                        { 
                            break;
                        }

                        map["PressAnyKey"].performed -= pressAnyKey.Execute;
                        map["PressAnyKey"].performed += pressAnyKey.Execute;
                    }
                    break;
                case "Locked":
                    {
                        // Locked 맵에는 특별한 액션이 없으므로 아무 것도 하지 않음
                    }
                    break;
                case "Restricted":
                    {
                        foreach (var action in map.actions)
                        {
                            string actionName = action.name;
                            if (actionName == "Menu")
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
                default:
                    {
                        hasAllocated = false;
                        Debug.LogWarning($"InputManager: Unhandled ActionMap '{map.name}'");
                    }
                    break;
            }
            if (hasAllocated)
            {
                Debug.Log($"[ActionMap] {map.name} has been allocated.");
            }
            ChangeActionMap("Locked"); // 초기 상태는 Locked
        }
    }

    /// <summary>
    /// Input ActionMap을 전환합니다.
    /// </summary>
    /// <param name="mapName"></param>
    private void ChangeActionMap(string mapName)
    {
        // 모든 액션맵을 비활성화 한 뒤 특정 액션맵만 활성화
        playerInput.DeactivateInput();
        playerInput.actions.Disable();
        playerInput.SwitchCurrentActionMap(mapName);
        foreach (var action in playerInput.currentActionMap.actions)
        {
            action.Reset();
        }
        playerInput.currentActionMap.Enable();
        playerInput.ActivateInput();
        currentActionMap = playerInput.currentActionMap;
    }

    /// <summary>
    /// InputMode를 ActionMap을 전환하여 입력 모드를 변경합니다.
    /// </summary>
    /// <param name="mode">변경할 모드</param>
    public void ChangeInputMode(InputMode mode)
    {
        if (currentInputMode == mode)
        {
            return;
        }

        currentInputMode = mode;

        switch (currentInputMode)
        {
            case InputMode.Locked:
                // 입력 잠금 상태에서의 처리
                {
                    ChangeActionMap("Locked");
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
                    ChangeActionMap("Restricted");
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

    public void RebindAction(string ActionButtonName, string newBindKey)
    {

    }

    public void ResetBindings()
    {

    }

    private static void GetAnyInput(InputControl control)
    {
        if (control == null)
        {
            isContextReady = true;
            if (captureAction.enabled)
            {
                captureAction.Disable();
            }
            return;
        }

        if (captureAction.enabled)
        {
            captureAction.Disable();
        }

        if (captureAction.bindings.Count == 0)
        {
            captureAction.AddBinding(control.path);
        }
        else
        {
            captureAction.ApplyBindingOverride(0, control.path);
        }

        isContextReady = false;
        captureAction.Enable();
    }

    public bool GetAnyKey()
    {
        if (isContextReady)
        {
            return true;
        }

        InitializeCaptureAction();

        isContextReady = false;

        if (!captureAction.enabled)
        {
            InputSystem.onAnyButtonPress.CallOnce(GetAnyInput);
            captureAction.Enable();
        }

        return false;
    }

    public InputAction.CallbackContext PeekLastInput()
    {
        return lastInputContext;
    }

    public void EndCapture()
    {
        isContextReady = false;

        if (captureAction != null)
        {
            if (captureAction.enabled)
            {
                captureAction.Disable();
            }

            captureAction.RemoveAllBindingOverrides();

            captureAction.Reset();
        }

        lastInputContext = default;
    }

    private static void OnCapturePerformedAction(InputAction.CallbackContext ctx)
    {
        lastInputContext = ctx;
        isContextReady = true;
    }
    #endregion Custom Methods
}
