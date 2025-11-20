using UnityEngine;
using static Types;

public class Menu : InteractiveUIBehaviour
{
    protected override void OnSubmit()
    {
        if (selectedButton == null)
        {
            return;
        }
        switch (selectedButton.gameObject.name)
        {
            case "ReturnButton":
                GameManager.instance.ChangeGameState(GameState.Playing);
                break;
            case "NewGameButton":
                GameManager.instance.ChangeGameState(GameState.Reset);
                break;
            case "ControlButton":
                UIManager.instance.Show("Control");
                break;
            case "OptionsButton":
                UIManager.instance.Show("Options");
                break;
            case "ExitButton":
                GameManager.instance.Exit();
                break;
            default:
                Debug.LogWarning($"Menu: Unhandled button '{selectedButton.gameObject.name}'");
                break;
        }
    }
}
