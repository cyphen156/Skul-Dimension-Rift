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

        string name = selectedButton.name;

        switch (name)
        {
            // 키 변경 요청
            // UI 호출이 아닌 게임매니저 호출인 이유는
            // ResourceManager, UIManager, GameManager, InputManager가 모두 필요하기 때문
            // 흐름이 일괄적으로 통제되게 하기 위해
            // 이 모두를 알 수 있는건 GameManager로 제한하고 싶음
            case "ReturnButton":
                GameManager.instance.ChangeGameState(GameState.Playing);
                break;
            case "ResetButton":
                GameManager.instance.ControlReBind(); // 전체 리셋
                break;
            default:
                GameManager.instance.ControlReBind(name); // 개별 리바인드
                break;
        }
    }
}
