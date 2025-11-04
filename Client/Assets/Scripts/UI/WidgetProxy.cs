using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WidgetProxy : InteractiveUIBehaviour
{
    [SerializeField] public Widget widget;
    [SerializeField] private List<Button> cachedButtons = new List<Button>();

    public void Bind(Widget w)
    {
        widget = w;
        RebuildSelectables();
    }

    private void RebuildSelectables()
    {
        buttons.Clear();
        cachedButtons.Clear();

        if (widget == null)
        {
            selectedButton = null;
            return;
        }

        if (widget.type == WidgetType.StepperWidget)
        {
            StepperWidget s = (StepperWidget)widget;

            if (s.leftArrow != null)
            {
                cachedButtons.Add(s.leftArrow);
            }
            if (s.rightArrow != null)
            {
                cachedButtons.Add(s.rightArrow);
            }
        }
        else if (widget.type == WidgetType.OneShotWidget)
        {
            OneShotWidget o = (OneShotWidget)widget;

            if (o.oneShotButton != null)
            {
                cachedButtons.Add(o.oneShotButton);
            }
        }
        else if (widget.type == WidgetType.SliderWidget)
        {
            SliderWidget sw = (SliderWidget)widget;

            if (sw.slider != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(sw.slider.gameObject);
                selectedButton = sw.slider; // Selectable
            }
        }

        if (cachedButtons.Count > 0)
        {
            foreach (var b in cachedButtons)
            {
                if (b != null)
                {
                    buttons.Add(b);
                }
            }

            selectedButton = buttons.Count > 0 ? buttons[0] : null;
            if (selectedButton != null)
            {
                selectedButton.Select();
            }
        }
    }

    protected override void OnSubmit()
    {
        if (widget == null)
        {
            UIManager.instance.Hide(gameObject.name);
            return;
        }

        switch (widget.type)
        {
            case WidgetType.StepperWidget:
                {
                    StepperWidget s = (StepperWidget)widget;
                    Button selBtn = selectedButton as Button;

                    if (selBtn == s.leftArrow)
                    {
                        // GameManager.instance.Stepper(s.groupKey, s.widgetName, -1);
                        return;
                    }

                    if (selBtn == s.rightArrow)
                    {
                        // GameManager.instance.Stepper(s.groupKey, s.widgetName, +1);
                        return;
                    }
                    return;
                }

            case WidgetType.OneShotWidget:
                {
                    OneShotWidget o = (OneShotWidget)widget;
                    Button selBtn = selectedButton as Button;

                    if (selBtn == o.oneShotButton)
                    {
                        // GameManager.instance.OneShot(o.groupKey, o.widgetName);
                        UIManager.instance.Hide(gameObject.name);
                    }
                    return;
                }

            case WidgetType.SliderWidget:
                {
                    // 슬라이더는 포커스만 주면 좌/우 네비로 값 변경됨.
                    return;
                }
        }
    }
}
