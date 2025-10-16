using Unity.Services.Matchmaker.Models;
using UnityEngine;


/// <summary>
/// 인풋 필드에 입력된 키를 해당 액션에 바인딩하는 팝업 UI
/// </summary>
public class PopUp : InteractiveUIBehaviour
{
    [SerializeField] private string actionName;
    protected override void OnSubmit()
    {
        if (selectedButton == null)
        {
            return;
        }
    }

    public void SetActionName(string name)
    {
        actionName = name;
    }
}
