using Assets.Scripts.Interface;
using System;
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
    private InputAction storedAction;
    private InputAction.CallbackContext storedInput;

    [SerializeField] private bool isPressed;        // 슬라이더를 위한 드래그
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
        isPressed = false;
    }

    private void Update()
    {
        // 슬라이더가 아니면 업데이트를 하지 않음
        if (!(widget is SliderWidget))
        {
            return;
        }

        if (isPressed && storedAction != null && storedAction.name == "Navigate")
        {
            HandleNavigate(storedInput);
            OnSubmit();
            return;
        }

        if (storedAction != null && storedAction.name == "Navigate")
        {
            isPressed = false;
            storedAction = null;
            storedInput = default;
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

        if (selectedButton == null)
        {
            foreach (Selectable button in buttons)
            {
                if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                {
                    continue;
                }
                RectTransform rect = (RectTransform)button.transform;
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, lastPoint))
                {
                    selectedButton = button;
                    break;
                }
            }

            if (selectedButton == null)
            {
                selectedButton = buttons[0];
            }
        }

        if (actionName == "Click" || actionName == "Navigate")
        {
            bool isSlider = selectedButton is Slider;

            if (isSlider)
            {
                isPressed = true;
                storedAction = ctx.action;
                storedInput = ctx;
            }
            else
            {
                isPressed = false;
                storedAction = null;
                storedInput = default;
                (widget as SliderWidget)?.ResetPrevPoint();
            }
        }

        bool suppressSubmit = false;

        if (ctx.performed)
        {
            switch (actionName)
            {
                case "Point":
                    {
                        HandlePoint(ctx);
                        if (!(selectedButton is Slider))
                        {
                            isPressed = false;
                            storedAction = null;
                            storedInput = default;
                            (widget as SliderWidget)?.ResetPrevPoint();

                            suppressSubmit = true;
                            break;
                        }

                        bool isHolding = isPressed && storedAction != null && storedAction.IsPressed();

                        if (!isHolding)
                        {
                            isPressed = false;
                            storedAction = null;
                            storedInput = default;
                            (widget as SliderWidget)?.ResetPrevPoint();
                            break;
                        }

                        if (isPressed)
                        {
                            SliderWidget sw = widget as SliderWidget;
                            if (sw != null)
                            {
                                sw.HandleSlider(lastPoint);
                            }
                        }
                        break;
                    }
                case "Click":
                    if (isPressed && selectedButton is Slider)
                    {
                        break;
                    }

                    foreach (Selectable button in buttons)
                    {
                        if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
                        {
                            continue;
                        }
                        RectTransform rect = (RectTransform)button.transform;
                        if (RectTransformUtility.RectangleContainsScreenPoint(rect, lastPoint))
                        {
                            selectedButton = button;
                            break;
                        }
                    }
                    break;
                case "Navigate":
                    HandleNavigate(ctx);
                    break;
                case "ScrollWheel":
                    // 슬라이더 스크롤의 경우 3배 적용
                    {
                        int value = 1;
                        if (widget is SliderWidget)
                        {
                            value = 3 * value;
                        }
                        for (int i = 0; i < value; ++i)
                        {
                            HandleNavigate(ctx);
                        }
                        break;
                    }
                case "Submit":
                case "RightClick":
                    break;
                default:
                    Debug.LogWarning($"{this.name}: Unhandled action '{actionName}'");
                    break;
            }
        }
        if (!suppressSubmit)
        {
            OnSubmit();
        }
    }

    protected override void OnSubmit()
    {
        // 값을 전달하고 내부에서 참조가 있으니 조회할 수 있음
        GameManager.instance.ApplyUserOptionSetting(this);
    }

    /// <summary>
    /// base와는 다르게 버튼그룹을 순회하지 않음
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
        
        // 슬라이더라면 네비게이션도 적용한다
        if (selectedButton is Slider)
        {
            SliderWidget sw = widget as SliderWidget;
            if (sw != null)
            {
                sw.HandleSlider(navigation, true);
            }
            return;
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
        selectedButton.Select();
    }

    public float GetValue()
    {
        if (selectedButton is Slider)
        {
            SliderWidget sw = widget as SliderWidget;
            if (sw != null)
            {
                return sw.slider.value;
            }
        }
        
        return (float)buttons.IndexOf(selectedButton);
    }

    public override void Refresh(string data)
    {
        widget.Refresh(data);
    }
}

[Serializable]
public class StepperWidget : IWidget
{
    public Button leftArrow;
    public Button rightArrow;
    public TMP_Text optionText;
    public Enum widgetKey;

    public void Refresh(string data)
    {
        optionText.text = data;
    }
}

[Serializable]
public class SliderWidget : IWidget
{
    public Slider slider;
    public float sliderSpeed = 0.8f;

    private Vector2 prevPoint;   // 이전 프레임 좌표
    private bool hasPrev;        // 초기화 여부

    public void Refresh(string data)
    {
       // 슬라이더의 경우 이미 포인터에 연동되어 처리되고 있으므로 아무것도 하지 않음
    }

    public void HandleSlider(Vector2 point, bool isNav = false)
    {
        if (slider == null)
        {
            return;
        }

        float newValue = slider.value;

        if (isNav)
        {
            // 네비게이션은 프레임 보정 + 속도 기반
            float delta = (point.x + point.y) * sliderSpeed * Time.deltaTime;
            newValue += delta;
        }
        else
        {
            RectTransform track = slider.fillRect != null
            ? slider.fillRect
            : slider.transform as RectTransform;

            if (track == null)
            {
                return;
            }

            if (hasPrev)
            {
                float delta = point.x - prevPoint.x;
                float trackWidth = track.rect.width;

                // 슬라이더 트랙 폭을 기준으로 이동 비율 계산
                float normalizedDelta = (delta / trackWidth);

                newValue += normalizedDelta * (slider.maxValue - slider.minValue);
            }

            prevPoint = point;
            hasPrev = true;
        }

        slider.value = Mathf.Clamp(newValue, slider.minValue, slider.maxValue);
    }

    public void ResetPrevPoint()
    {
        hasPrev = false;
    }
}

[Serializable]
public class OneShotWidget : IWidget
{
    public Button oneShotButton;
    public TMP_Text optionText;
    public void Refresh(string data)
    {
        // oneShotButton의 경우 단발성 클릭 이벤트를 진행하므로 아무것도 적용하지 않음
    }
}