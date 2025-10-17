using Assets.Scripts.Interface;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static Types;

public class PressAnyKey : MonoBehaviour, IInteractive
{
    private TextMeshProUGUI text;
    [SerializeField] private string originalText;

    private void Awake()
    {
        text = transform.GetComponentInChildren<TextMeshProUGUI>();
        originalText = text.text;
    }

    private void Start()
    {
        if (Gamepad.current != null)
        {
            string layout = Gamepad.current.layout.ToLower();

            if (layout.Contains("dualshock") || layout.Contains("dualSense") || layout.Contains("playstation"))
            {
                originalText = "- X 버튼을 누르세요 -";
            }
            else
            {
                originalText = "- A키를 누르세요 -";
            }
        }
        else if (Touchscreen.current != null)
        {
            originalText = "- 아무 곳이나 터치하세요 -";
        }
        else
        {
            originalText = "- 아무키나 누르세요 -";
        }
        text.text = originalText;
    }

    public void Execute(InputAction.CallbackContext ctx)
    {
        OnPressed(ctx);
    }

    private void OnPressed(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            GameManager.instance.ChangeGameState(GameState.Playing);
        }
    }
}
