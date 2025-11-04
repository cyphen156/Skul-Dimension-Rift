using Assets.Scripts.Interface;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum WidgetType
{
    StepperWidget,
    SliderWidget,
    OneShotWidget
}

public class Widget : MonoBehaviour
{
    public string groupKey;
    public string widgetName;
    public WidgetType widgetType;
    public IWidget widget;

    protected void Awake()
    {
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
    }
}

public class StepperWidget : IWidget
{
    public Button leftArrow;
    public Button rightArrow;
    public TMP_Text optionText;
}

public class SliderWidget : IWidget
{
    public Slider slider;
}

public class OneShotWidget : IWidget
{
    public Button oneShotButton;
    public TMP_Text optionText;
}