using Assets.Scripts.Interface;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Menu : MonoBehaviour, IUIInputHandler
{
    [SerializeField] private List<Button> buttons;
    [SerializeField] private Button selectedButton;
    private void Awake()
    {
        buttons = ComponentRegistrar.RegisterComponentsInChildren<Button>(transform, includeInactive: true);
    }

    // 항상 활성화 된다면 선택된 버튼 요소를 첫 요소로 지정
    private void OnEnable()
    {
        if (buttons.Count > 0)
        {
            selectedButton = buttons[0];
            selectedButton.Select();
        }
    }

    public void Execute(InputAction.CallbackContext ctx)
    {
        if (ctx.canceled)
        {
            string actionName = ctx.action.name;
            switch (actionName)
            {
                case "Navigate":
                    Vector2 navigation = ctx.ReadValue<Vector2>();
                    int currentIndex = buttons.IndexOf(selectedButton);
                    if (navigation.y > 0) // 위로 이동
                    {
                        currentIndex = (currentIndex - 1 + buttons.Count) % buttons.Count;
                    }
                    else if (navigation.y < 0) // 아래로 이동
                    {
                        currentIndex = (currentIndex + 1) % buttons.Count;
                    }
                    selectedButton = buttons[currentIndex];
                    selectedButton.Select();
                    break;
                case "Submit":
                    if (selectedButton != null)
                    {
                        selectedButton.onClick.Invoke();
                    }
                    break;
                default:
                    Debug.LogWarning($"Menu: Unhandled action '{actionName}'");
                    break;
            }
        }
    }
}
