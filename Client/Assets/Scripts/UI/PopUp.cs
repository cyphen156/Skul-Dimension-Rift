using Assets.Scripts.Interface;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 인풋 필드에 입력된 키를 해당 액션에 바인딩하는 팝업 UI
/// </summary>
/// 
public class PopUp : InteractiveUIBehaviour, IInteractive
{
    [SerializeField] private string inputKey;
    [SerializeField] private InputControl bindingControl;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private bool isSubmitted;

    private new void Awake()
    {
        if (inputField == null)
        {
            inputField = GetComponentInChildren<TMP_InputField>(includeInactive: true);
        }
        inputField.readOnly = true;
    }

    private new void OnEnable()
    {
        inputKey = string.Empty;
        inputField.text = string.Empty;
        inputField.Select();
        inputField.ActivateInputField();
        isSubmitted = false;
    }

    /// <summary>
    /// 전달받은 입력을 화면에 갱신합니다.
    /// </summary>
    void IInteractive.Execute(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        if (ctx.action.name == "Submit")
        {
            OnSubmit();
            return;
        }

        bindingControl = ctx.control;
        inputKey = bindingControl.displayName;
        inputField.text = inputKey;
    }

    protected override void OnSubmit()
    {
        isSubmitted = true;
        UIManager.instance.Hide(name);
    }

    public InputControl GetBindingControl()
    {
        return bindingControl;
    }

    public bool CheckConfirm()
    {
        return isSubmitted;
    }
}
