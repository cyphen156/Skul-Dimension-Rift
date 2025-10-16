using static Types;

// 컨트롤 전환 UI를 관리하는 클래스입니다.
// 액션에 바인드된 키를 변경을 위해 Popup UI를 호출하거나
// 처음 상태로 리셋하는 기능을 담당합니다.
public class Control : InteractiveUIBehaviour
{
    protected override void OnSubmit()
    {
        if (selectedButton == null)
        {
            return;
        }
        if (selectedButton.name == "ResetButton")
        {
        }
        else if (selectedButton.name == "ReturnButton")
        {
            GameManager.instance.ChangeGameState(GameState.Playing);
        }
        else
        {
            UIManager.instance.Show("PopUp");
        }
    }
}
