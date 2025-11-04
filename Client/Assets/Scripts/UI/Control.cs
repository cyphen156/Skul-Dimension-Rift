using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Types;

// 컨트롤 전환 UI를 관리하는 클래스입니다.
// 액션에 바인드된 키를 변경을 위해 Popup UI를 호출하거나
// 처음 상태로 리셋하는 기능을 담당합니다.
public class Control : InteractiveUIBehaviour
{
    [SerializeField] private Dictionary<string, Image> controlButtons = new Dictionary<string, Image>();

#if UNITY_EDITOR
    [SerializeField] private List<Image> images = new List<Image>();
#endif

    private new void Awake()
    {
        base.Awake();

        controlButtons.Clear();
        foreach (Button button in buttons)
        {
            string key = button.name.Replace("Button", "");
            if (key == "Reset" || key == "Return")
            {
                continue;
            }

            GameObject controlImage = button.transform.Find("ControlImage").gameObject;
            GameObject buttonIcon = controlImage.transform.Find("ButtonIcon").gameObject;
            controlButtons[key] = buttonIcon.GetComponent<Image>();
#if UNITY_EDITOR
            Image item = null;
            controlButtons.TryGetValue(key, out item);
            images.Add(item);
#endif
        }
    }

    private void Start()
    {
        Refresh();
    }

    protected override void OnSubmit()
    {
        if (selectedButton == null)
        {
            return;
        }

        string name = selectedButton.name.Replace("Button", "");

        switch (name)
        {
            // 키 변경 요청
            // UI 호출이 아닌 게임매니저 호출인 이유는
            // ResourceManager, UIManager, GameManager, InputManager가 모두 필요하기 때문
            // 흐름이 일괄적으로 통제되게 하기 위해
            // 이 모두를 알 수 있는건 GameManager로 제한하고 싶음
            case "Return":
                UIManager.instance.Hide(gameObject.name);
                break;
            case "Reset":
                GameManager.instance.ControlReBind(); // 전체 리셋
                break;
            default:
                GameManager.instance.ControlReBind(name); // 개별 리바인드
                break;
        }
    }

    /// <summary>
    /// 리프레시 시 스프라이트를 직접 가져오는 로직을 수정할 필요가 있음
    /// 매번 리프레시 할때마다 스프라이트를 가져오는건 캐싱된 데이터라 할지라도 좀 부담스러움
    /// 그래서 딕셔너리 키값에 저장된 밸류를 검사해서 다르면 Change - Apply를 적용하는것이 합리적임
    /// 이 부분의 장점이 추후 생길 애니메이트 이벤트에 대해 마우스 포인터와 관련하여 동적 이미지 교체가 포함되어있음
    /// 하이라이트, 클릭, 호버시 스프라이트가 _White로 교체되어야 함
    /// </summary>
    /// <param name="key"></param>
    public override void Refresh(string key = null)
    {
        if (!string.IsNullOrEmpty(key))
        {
            if (controlButtons.TryGetValue(key, out var img))
            {
                var sprite = ResourceManager.instance.GetControlSprite(key);
                // sprite가 널이 올 수 있다
                img.sprite = sprite;
                img.preserveAspect = true;
            }
            return;
        }

        foreach (var kv in controlButtons)
        {
            var img = kv.Value;
            if (!img) continue;

            var sp = ResourceManager.instance.GetControlSprite(kv.Key);
            img.sprite = sp;
            img.preserveAspect = true;
        }
    }
}
