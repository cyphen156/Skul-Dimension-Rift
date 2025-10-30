using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using static Types;

/// <summary>
/// 사용자의 입력을 관리하는 싱글톤 매니저 클래스입니다.
/// </summary>
public class InputManager : NetworkBehaviour
{
    public static InputManager instance;

    [SerializeField] private InputMode currentInputMode;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private InputActionMap currentActionMap;
    [SerializeField] private InputActionMap playerMap;
    // 자주 사용되는 플레이어 액션 맵은 조회 횟수를 감소시키기 위하여 레퍼런스로 저장합니다.
#if UNITY_EDITOR
    [System.Serializable]
    private struct OverrideView
    {
        public string action;
        public string binding;
    }
    [SerializeField] private List<OverrideView> currentOverrideActions = new List<OverrideView>();
#endif

    private static readonly Dictionary<string, string> moveAction =
    new()
    {
        { "MoveUp", "up" },
        { "MoveDown", "down" },
        { "MoveLeft", "left" },
        { "MoveRight", "right" },
    };
    private static readonly HashSet<string> NonRebindables = new(StringComparer.Ordinal)
    {
        "ArrowDash",
    };
    private static InputAction captureAction;
    private static InputAction.CallbackContext lastInputContext;
    private static bool isContextReady;
    private static string currentActivatedDevice;

    private bool hasInitialized;

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
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }
    }

    private void OnEnable()
    {
        if (!hasInitialized)
        {
            InitializeInput();
            hasInitialized = true;
        }

        if (playerInput != null && playerInput.currentActionMap != null)
        {
            playerInput.currentActionMap.Enable();
        }

        if (playerInput != null)
        {
            playerInput.onControlsChanged -= OnDeviceChanged;
            playerInput.onControlsChanged += OnDeviceChanged;
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
        
        if (playerInput != null)
        {
            playerInput.onControlsChanged -= OnDeviceChanged;
        }
    }

    private void Start()
    {
        if (!hasInitialized)
        {
            InitializeInput();
            hasInitialized = true;
        }
    }
#endregion Unity Methods

    private void InitializeInput()
    {
        playerInput.actions = ResourceManager.instance.GetUserInputActions();
        playerMap = playerInput.actions.FindActionMap("Player");
        AllocateInputActions();
        ChangeInputMode(InputMode.Locked);

        currentActivatedDevice = playerInput.currentControlScheme;
        ResourceManager.instance.ChangeResource("controlSprites", currentActivatedDevice);
        PopulateControlBindings();
#if UNITY_EDITOR
        currentOverrideActions.Clear();
        for (int i = 0; i < playerMap.actions.Count; i++)
        {
            var act = playerMap.actions[i];
            var v = new OverrideView();
            v.action = act.name;
            v.binding = act.GetBindingDisplayString();
            currentOverrideActions.Add(v);
        }
#endif
    }

    private void OnDeviceChanged(PlayerInput pi)
    {
        string newScheme = pi.currentControlScheme;

        if (string.Equals(newScheme, currentActivatedDevice, StringComparison.Ordinal))
        {
            return;
        }

        currentActivatedDevice = newScheme;

        ResourceManager.instance.ChangeResource("controlSprites", currentActivatedDevice);

        PopulateControlBindings();
#if UNITY_EDITOR
        Debug.Log("Input Device Changed");
#endif
    }

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
                                if (actionName == "Menu" || actionName == "Interaction")
                                {
                                    action.performed -= UIManager.instance.Execute_Internal;
                                    action.performed += UIManager.instance.Execute_Internal;
                                }
                                else
                                {
                                    action.performed -= playerController.Execute;
                                    action.performed += playerController.Execute;
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
                            if (actionName == "Menu" || actionName == "Interaction")
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

                        map["PressAnyKey"].performed -= UIManager.instance.Execute;
                        map["PressAnyKey"].performed += UIManager.instance.Execute;
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
                            if (actionName == "Menu" || actionName == "Interaction")
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
                        Debug.LogWarning($"InputManager: Unhandled ActionMap '{map.name}'");
                    }
                    break;
            }
        }
        ChangeActionMap("Locked");
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

    /// <summary>
    /// 액션의 바인딩을 리바인드하는 함수
    /// </summary>
    /// <param name="actionButtonName">새 바인딩을 할당할 액션의 이름</param>
    /// <param name="newBindKey">만약 null일 경우 키 할당을 해제합니다.</param>
    /// <param name="swapButtonName"></param>
    public void RebindAction(string actionButtonName, out string swapButtonName, InputControl newBindKey = null)
    {
        swapButtonName = null;

        string newPath = newBindKey != null ? newBindKey.path : string.Empty;

        if (string.IsNullOrEmpty(actionButtonName) || playerMap == null)
        {
            return;
        }

        // 맵에서 버튼 이름에 해당하는 액션 이름 찾기
        string targetActionName = actionButtonName;
        string targetPartName = null;

        if (NonRebindables.Contains(targetActionName))
        {
            Debug.LogWarning("현재 비활성화된 액션입니다.");
            return;
        }

        if (actionButtonName.StartsWith("Move", StringComparison.OrdinalIgnoreCase))
        {
            targetActionName = "Move";

            if (moveAction.ContainsKey(actionButtonName))
            {
                targetPartName = moveAction[actionButtonName];
            }
            else
            {
                string suffix = actionButtonName.Substring("Move".Length);
                if (!string.IsNullOrEmpty(suffix))
                {
                    targetPartName = suffix.ToLowerInvariant();
                }
            }
        }

        InputAction action = playerInput.actions.FindAction(targetActionName);
        if (action == null)
        {
            return;
        }


        // 대상 바인딩 인덱스 찾기
        int targetIndex = -1;

        if (!string.IsNullOrEmpty(targetPartName))
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];

                if (!binding.isPartOfComposite)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(binding.name))
                {
                    continue;
                }

                if (!binding.name.Equals(targetPartName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(currentActivatedDevice) && !string.IsNullOrEmpty(binding.groups))
                {
                    if (!GroupsContain(binding.groups, currentActivatedDevice))   
                    {
                        continue;
                    }
                }

                targetIndex = i;
                break;
            }
        }

        if (targetIndex == -1)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];

                if (binding.isComposite)
                {
                    continue;
                }

                if (binding.isPartOfComposite)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(currentActivatedDevice) && !string.IsNullOrEmpty(binding.groups))
                {
                    if (!GroupsContain(binding.groups, currentActivatedDevice))
                    {
                        continue;
                    }
                }

                targetIndex = i;
                break;
            }
        }

        if (targetIndex == -1)
        {
            return;
        }

        if (newBindKey == null)
        {
            action.ApplyBindingOverride(targetIndex, string.Empty);
            ResourceManager.instance.ApplyControlBinding(actionButtonName, string.Empty);
            return;
        }

        InputAction foundedAction = null;
        int foundedIndex = -1;

        foreach (InputAction act in playerMap.actions)
        {
            int count = act.bindings.Count;

            for (int i = 0; i < count; i++)
            {
                InputBinding binding = act.bindings[i];

                if (binding.isComposite)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(currentActivatedDevice) && !string.IsNullOrEmpty(binding.groups))
                {
                    if (!GroupsContain(binding.groups, currentActivatedDevice))
                    {
                        continue;
                    }
                }

                var pathOnBinding = string.IsNullOrEmpty(binding.effectivePath) ? binding.path : binding.effectivePath;
                if (!string.IsNullOrEmpty(pathOnBinding) && Norm(pathOnBinding) == Norm(newPath))
                {
                    if (NonRebindables.Contains(act.name))
                    {
                        continue;
                    }

                    foundedAction = act;
                    foundedIndex = i;
                    break;
                }
            }

            if (foundedAction != null)
            {
                break;
            }
        }

        if (foundedAction != null)
        {
            bool sameAction = foundedAction == action;
            bool sameIndex = sameAction && foundedIndex == targetIndex;

            if (sameIndex)
            {
                return;
            }
            string oldPathOnTarget = string.IsNullOrEmpty(action.bindings[targetIndex].effectivePath)
                ? action.bindings[targetIndex].path
                : action.bindings[targetIndex].effectivePath;
            if (oldPathOnTarget == null)
            {
                oldPathOnTarget = string.Empty;
            }

            foundedAction.ApplyBindingOverride(foundedIndex, oldPathOnTarget);
            string foundedAlias = foundedAction.name;
            var foundedBinding = foundedAction.bindings[foundedIndex];
            if (foundedBinding.isPartOfComposite && !string.IsNullOrEmpty(foundedBinding.name))
            {
                foundedAlias = "Move" + char.ToUpper(foundedBinding.name[0]) + foundedBinding.name.Substring(1);
            }
            swapButtonName = foundedAlias;
            ResourceManager.instance.ApplyControlBinding(foundedAlias, ParseKey(foundedAlias));
        }

        action.ApplyBindingOverride(targetIndex, newPath);
        ResourceManager.instance.ApplyControlBinding(actionButtonName, ParseKey(actionButtonName));

#if UNITY_EDITOR
        currentOverrideActions.Clear();
        for (int i = 0; i < playerMap.actions.Count; i++)
        {
            var act = playerMap.actions[i];
            OverrideView view = new OverrideView();
            view.action = act.name;
            view.binding = act.GetBindingDisplayString();
            currentOverrideActions.Add(view);
        }
#endif
    }

    public void ResetBindings()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            return;
        }

        playerInput.DeactivateInput();
        playerInput.actions.Disable();

        playerMap.RemoveAllBindingOverrides();

        playerInput.actions.Enable();
        playerInput.ActivateInput();
        PopulateControlBindings();

#if UNITY_EDITOR
        currentOverrideActions.Clear();
        for (int i = 0; i < playerMap.actions.Count; i++)
        {
            var act = playerMap.actions[i];
            OverrideView view = new OverrideView();
            view.action = act.name;
            view.binding = act.GetBindingDisplayString();
            currentOverrideActions.Add(view);
        }
#endif
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
            captureAction.performed -= OnCapturePerformedAction;
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

    #region Utility
    // 외부에서 들어온 키를 파싱해서 리소스 매니저에게 넘겨주기 위한 파서
    // 리소스 매니저는 이 것을 기반으로 맞는 컨트롤 키에 해당하는 스프라이트를 조회할 수 있게한다.
    public string ParseKey(string key)
    {
        InputAction action = playerMap?.FindAction(key, false);

        if (action == null)
        {
            if (key.StartsWith("Move", StringComparison.OrdinalIgnoreCase))
            {
                action = playerMap.FindAction("Move", false);
            }
            else
            {
                Debug.LogWarning($"Action '{key}' not found in Player map.");
                return null;
            }
        }
        string displayString = action.GetBindingDisplayString();

        if (action.name == "Move")
        {
            string part = key.Replace("Move", "");

            for (int i = 0; i < action.bindings.Count; ++i)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isPartOfComposite == true && binding.name.Equals(part, StringComparison.OrdinalIgnoreCase))
                {
                    displayString = string.IsNullOrEmpty(binding.effectivePath) ? binding.path : binding.effectivePath;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(displayString))
        {
            Debug.LogWarning($"[InputManager] Action '{key}' has no x binding display string.");
            return null;
        }

        string[] tokens = displayString.Split('/');
        string controlKey = tokens.Length > 1 ? tokens[^1] : displayString;

        // 강제분할 1회 추가
        tokens = controlKey.Split();
        controlKey = tokens.Length > 1 ? tokens[^1] : controlKey;
        controlKey = controlKey.Trim();
        controlKey = char.ToUpper(controlKey[0]) + controlKey.Substring(1);

        return controlKey;
    }

    private void PopulateControlBindings()
    {
        if (playerMap == null)
        {
            return;
        }

        foreach (var action in playerMap.actions)
        {
            if (NonRebindables.Contains(action.name))
            {
                continue;
            }

            if (action.name == "Move")
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];
                    if (binding.isPartOfComposite == true && !string.IsNullOrEmpty(binding.name))
                    {
                        string alias = "Move" + char.ToUpper(binding.name[0]) + binding.name.Substring(1);
                        string normalized = ParseKey(alias);
                        if (string.IsNullOrEmpty(normalized))
                        {
                            normalized = string.Empty;
                        }
                        ResourceManager.instance.ApplyControlBinding(alias, normalized);
                    }
                }
            }
            else
            {
                string normalized = ParseKey(action.name);
                if (string.IsNullOrEmpty(normalized))
                {
                    normalized = string.Empty;
                }
                ResourceManager.instance.ApplyControlBinding(action.name, normalized);
            }
        }
    }

    private static bool GroupsContain(string groups, string scheme)
    {
        if (string.IsNullOrEmpty(scheme))
        {
            return true;
        }
        if (string.IsNullOrEmpty(groups))
        {
            return false;
        }

        var tokens = groups.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            if (string.Equals(token.Trim(), scheme.Trim(), StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static string Norm(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }
        return path.Trim().ToLowerInvariant()
                   .Replace("gamepad:", "gamepad/")
                   .Replace("<", "").Replace(">", "");
    }
    #endregion
}
