using Assets.Scripts.Interface;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Types;

public class InteractiveUIBehaviour : MonoBehaviour, IInteractive
{
    [SerializeField] protected List<Button> buttons;
    [SerializeField] protected Button selectedButton;
    [SerializeField] protected Vector2 lastPoint;
    protected void Awake()
    {
        buttons = ComponentRegistrar.RegisterComponentsInChildren<Button>(transform, includeInactive: true);
    }

    // 항상 활성화 된다면 선택된 버튼 요소를 첫 요소로 지정
    protected void OnEnable()
    {
        if (buttons.Count > 0)
        {
            selectedButton = buttons[0];
            lastPoint = selectedButton.transform.position;
            selectedButton.Select();
        }
    }

    public void Execute(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        string actionName = ctx.action.name;
        switch (actionName)
        {
            case "Point":
                HandlePoint(ctx);
                break;
            case "Click":
                foreach (Button button in buttons)
                {
                    if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                        continue;

                    RectTransform rect = button.GetComponent<RectTransform>();
                    if (RectTransformUtility.RectangleContainsScreenPoint(rect, lastPoint))
                    {
                        selectedButton = button;
                        OnSubmit();
                        break;
                    }
                }
                break;
            case "Navigate":
                HandleNavigate(ctx);
                break;
            case "RightClick":
                selectedButton = null;
                break;
            case "ScrollWheel":
                HandleNavigate(ctx);
                break;
            case "Submit":
                OnSubmit();
                break;
            default:
                Debug.LogWarning($"{this.name}: Unhandled action '{actionName}'");
                break;
        }
    }

    protected void HandlePoint(InputAction.CallbackContext ctx)
    {
        Vector2 pointerPosition = ctx.ReadValue<Vector2>();
        if (pointerPosition != lastPoint)
        {
            lastPoint = pointerPosition;

            // Raycast to find the button under the pointer
            foreach (Button button in buttons)
            {
                RectTransform rectTransform = button.GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerPosition))
                {
                    selectedButton = button;
                    selectedButton.Select();
                    break;
                }
            }
        }
    }

    protected void HandleNavigate(InputAction.CallbackContext ctx)
    {
        if (buttons.Count == 0)
        {
            return;
        }
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
    }

    protected virtual void OnSubmit()
    {
    }
}