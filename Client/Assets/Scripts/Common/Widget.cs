using Assets.Scripts.Interface;
using System;
using System.Diagnostics.Eventing.Reader;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Types;

public enum WidgetType
{
    None, // set as default
    StepperWidget,
    SliderWidget,
    OneShotWidget
}

/// <summary>
/// 위젯 클래스에 대한 기본 전제
/// 부모는 자식의 이름과 소속을 지정할 수 있다.
/// 다만 생성되는 자식의 타입은 자식 스스로가 이름규약에 의해 정한다.
/// 인간에 빗대어 보아 부모는 
/// 태아의 성별을 초음파 검사를 하기 전까지 알 수 없다는 점에서 착안한다.
/// </summary>
[Serializable]
public class Widget : InteractiveUIBehaviour
{
    public Enum groupKey;
    public string parentName;       // 위젯이 달린 오브젝트 이름 -> 종속성을 가지고 있음
    public WidgetType widgetType;   // 리플렉션 방지용
    public IWidget widget;
    public float value;

#if UNITY_EDITOR
    [SerializeField] private string groupName;
#endif
    protected new void Awake()
    {
        base.Awake();

        /// 위젯이 자신에 대한 타입 결정권은 부모가 아닌 
        /// 스스로 결정한다. 
        /// 부모는 자식의 타입을 알지 못한다는 원칙을 적용
        //if (widgetType == WidgetType.None)    /// 인스펙터상에서 설정할 수 있도록 할지는 추후 고민대상
        {
            string type = gameObject.name;
            switch (type)
            {
                case "StepperWidget":
                    widgetType = WidgetType.StepperWidget;
                    widget = new StepperWidget();
                    break;
                case "SliderWidget":
                    widgetType = WidgetType.SliderWidget;
                    widget = new SliderWidget();
                    break;
                case "OneShotWidget":
                    widgetType = WidgetType.OneShotWidget;
                    widget = new OneShotWidget();
                    break;
                default:
                    Debug.Log("object Name missMatch with WidgetType");
                    break;
            }
        }
    }

    /// <summary>
    /// 원래 Awake에서 자의적으로 부모에서 자신의 소속 그룹을 선택하던 기능이었지만,
    /// 어떤 부모가 올지 알수 없으므로
    /// 원래 위젯의 목적에 맞도록 부모관련 설정은 외부에 공개 키값과 타입을 받아 내부에 탑재하도록 변경
    /// 자식은 부모에게서 자신의 소속과 이름을 주입받을 수 있음
    /// </summary>
    /// <param name="groupKey"></param>
    /// <param name="parentName"></param>
    public void SetWidget(Enum groupKey, string parentName)
    {
        this.groupKey = groupKey;
        this.parentName = parentName;

#if UNITY_EDITOR
        groupName = groupKey.ToString();
#endif
    }

    public override void Execute(InputAction.CallbackContext ctx)
    {
        string actionName = ctx.action.name;

        // Point는 이미 프록시에서 처리되었음
        // 클릭의 경우 현재 포지션에 해당하는 버튼을 검증할 필요가 있음
        // 네비게이트의 경우는 재정렬 필요 버튼 인덱스를 루프하지 않도록 조정
        // => 방향이 바뀌엇을 때만 +- 값 조정
        // ==> 인덱스가 아니라 그냥 float의 형태로 위젯에+-1 전달
        switch (actionName)
        {
            case "Click":
                HandlePoint(ctx);
                break;
            case "Navigate":
            case "ScrollWheel":
                HandleNavigate(ctx);
                break;
            // 아래 기능들은 동작을 차단
            case "Point":
            case "RightClick":
                break;
            // Submit의 경우 항상 동작하도록 변경
            case "Submit":
                break;
            default:
                Debug.LogWarning($"{this.name}: Unhandled action '{actionName}'");
                break;
        }
        OnSubmit();
    }

    protected override void OnSubmit()
    {
        GameManager.instance.ApplyUserOptionSetting(this);
        widget?.OnSubmit();
    }

    /// <summary>
    /// base와는 다르게 버튼그룹을 순회하지 않음
    /// 대신 인덱스를 증감시키거나 인덱스가 1개인 경우 flaotvalue를 1 또는 -1로 변경함
    /// 이걸 다시 하위 위젯에게 전파
    /// </summary>
    /// <param name="ctx"></param>
    protected override void HandleNavigate(InputAction.CallbackContext ctx)
    {
        if (buttons.Count == 0)
        {
            return;
        }

        Vector2 navigation = ctx.ReadValue<Vector2>().normalized;

        if (navigation.x == 0 && navigation.y == 0)
        {
            return;
        }
        
        if (selectedButton == null)
        {
            selectedButton = buttons[0];
        }

        int currentIndex = -1;
        if (selectedButton is Selectable button)
        {
            currentIndex = buttons.IndexOf(button);
        }

        if (navigation.x > 0 || navigation.y > 0) // 우, 위로 이동, 1사분면
        {
            currentIndex++;
        }
        else if (navigation.x < 0 || navigation.y < 0) // 좌, 아래로 이동, 3사분면
        {
            currentIndex--;
        }
        currentIndex = Mathf.Clamp(currentIndex, 0, buttons.Count - 1);
        selectedButton = buttons[currentIndex];
        Debug.Log(currentIndex);
        selectedButton.Select();
    }
}

[Serializable]
public class StepperWidget : IWidget
{
    public Button leftArrow;
    public Button rightArrow;
    public TMP_Text optionText;

    public void OnSubmit()
    {

    }
}

[Serializable]
public class SliderWidget : IWidget
{
    public Slider slider;

    public void OnSubmit()
    {
    }
}

[Serializable]
public class OneShotWidget : IWidget
{
    public Button oneShotButton;
    public TMP_Text optionText;
    public void OnSubmit()
    {

    }
}