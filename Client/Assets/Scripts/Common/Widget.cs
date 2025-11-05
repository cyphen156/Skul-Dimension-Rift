using Assets.Scripts.Interface;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum WidgetType
{
    StepperWidget,
    SliderWidget,
    OneShotWidget
}

[Serializable]
public class Widget : InteractiveUIBehaviour
{
    public string groupKey;
    public string buttonName;
    public WidgetType widgetType;
    public IWidget widget;

    protected new void Awake()
    {
        base.Awake();

        string type = transform.gameObject.name;
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

        var parent = transform.parent;

        if (parent != null)
        {
            var buttonName = parent.name;
            if (buttonName.EndsWith("Button"))
            {
                this.buttonName = buttonName.Substring(0, buttonName.Length - "Button".Length);
            }
            else if (buttonName.EndsWith("Slider"))
            {
                this.buttonName = buttonName.Substring(0, buttonName.Length - "Slider".Length);
            }

            var group = parent.parent;
            if (group != null)
            {
                var groupName = group.name;
                if (!string.IsNullOrEmpty(groupName) && groupName.EndsWith("ButtonGroup"))
                {
                    groupKey = groupName.Substring(0, groupName.Length - "ButtonGroup".Length);
                }
            }
        }
    }
}

[Serializable]
public class StepperWidget : IWidget
{
    public Button leftArrow;
    public Button rightArrow;
    public TMP_Text optionText;
}

[Serializable]
public class SliderWidget : IWidget
{
    public Slider slider;
}

[Serializable]
public class OneShotWidget : IWidget
{
    public Button oneShotButton;
    public TMP_Text optionText;
}