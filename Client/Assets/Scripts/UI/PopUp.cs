using TMPro;
using UnityEngine;

/// <summary>
/// 인풋 필드에 입력된 키를 해당 액션에 바인딩하는 팝업 UI
/// </summary>
/// 
public class PopUp : InteractiveUIBehaviour
{
    [SerializeField] private string inputKey;
    [SerializeField] private TMP_InputField inputField;

    private new void Awake()
    {
        if (inputField == null)
        {
            inputField = GetComponentInChildren<TMP_InputField>(includeInactive: true);
        }

    }

    private new void OnEnable()
    {
        inputKey = string.Empty;
    }

    /// <summary>
    /// 입력된 키를 반환
    /// </summary>
    protected override void OnSubmit()
    {
        if (inputField == null)
        {
            return;
        }
        inputKey = inputField.text;
        UIManager.instance.Hide("PopUp");
    }

    internal string GetInputKey()
    {
        return inputKey;
    }
}
