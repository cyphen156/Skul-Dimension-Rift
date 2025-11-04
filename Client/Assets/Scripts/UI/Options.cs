using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Options : InteractiveUIBehaviour
{
    private readonly Dictionary<string, Widget> widgets = new Dictionary<string, Widget>();

#if UNITY_EDITOR
    [SerializeField] private List<Widget> debugWidgets;
#endif

    private new void Awake()
    {
        base.Awake();
        AllocateChildren();
    }

    protected new void OnEnable()
    {
        base.OnEnable();
    }

    protected new void OnDisable()
    {
        base.OnDisable();
    }

    private void AllocateChildren()
    {
        widgets.Clear();
#if UNITY_EDITOR
        debugWidgets?.Clear();
#endif

        foreach (Button button in buttons)
        {
            if (button == null)
            {
                continue;
            }

            Transform group = button.transform.parent;
            if (group == null)
            {
                continue;
            }

            string groupKey = group.name.Replace("ButtonGroup", "");
            string widgetName = button.name.Replace("Button", "");

            List<Transform> all = ComponentRegistrar.RegisterComponentsInChildren<Transform>(
                button.transform, 0, 0, 0, true, true
            );

            Transform optionButtons = null;
            Transform optionSlider = null;

            foreach (Transform t in all)
            {
                if (t == null)
                {
                    continue;
                }
                if (t.name == "OptionButtons")
                {
                    optionButtons = t;
                }
                else if (t.name == "OptionSlider")
                {
                    optionSlider = t;
                }
            }

            if (optionButtons != null)
            {
                Transform leftT = null;
                Transform rightT = null;
                Transform oneShotT = null;
                TMP_Text optionText = null;

                List<Transform> ob = ComponentRegistrar.RegisterComponentsInChildren<Transform>(
                    optionButtons, 0, 0, 0, true, true
                );

                foreach (Transform t in ob)
                {
                    if (t == null)
                    {
                        continue;
                    }

                    if (t.name == "LeftArrowButton" || t.name == "LeftArrow")
                    {
                        leftT = t; continue;
                    }
                    if (t.name == "RightArrowButton" || t.name == "RightArrow")
                    {
                        rightT = t; continue;
                    }
                    if (t.name == "OneShotButton" || t.name == "OneShotButtonImage")
                    {
                        oneShotT = t; continue;
                    }
                    if (t.name == "OptionText")
                    {
                        var txt = t.GetComponent<TMP_Text>();
                        if (txt != null)
                        {
                            optionText = txt;
                        }
                    }
                }

                if (leftT != null && rightT != null)
                {
                    StepperWidget step = new StepperWidget();
                    step.groupKey = groupKey;
                    step.widgetName = widgetName;
                    step.type = WidgetType.StepperWidget;
                    step.leftArrow = leftT.GetComponent<Button>();
                    step.rightArrow = rightT.GetComponent<Button>();
                    step.optionText = optionText;

                    widgets[widgetName] = step;
#if UNITY_EDITOR
                    debugWidgets?.Add(step);
#endif
                    continue;
                }

                if (oneShotT != null)
                {
                    OneShotWidget one = new OneShotWidget();
                    one.groupKey = groupKey;
                    one.widgetName = widgetName;
                    one.type = WidgetType.OneShotWidget;
                    one.oneShotButton = oneShotT.GetComponent<Button>();
                    one.optionText = optionText;

                    widgets[widgetName] = one;
#if UNITY_EDITOR
                    debugWidgets?.Add(one);
#endif
                    continue;
                }
            }

            if (optionSlider != null)
            {
                Slider slider = optionSlider.GetComponent<Slider>();
                if (slider == null)
                {
                    var found = ComponentRegistrar.RegisterComponentsInChildren<Slider>(optionSlider, 0, 0, 0, true, true);
                    if (found.Count > 0)
                    {
                        slider = found[0];
                    }
                }

                if (slider != null)
                {
                    SliderWidget sw = new SliderWidget();
                    sw.groupKey = groupKey;
                    sw.widgetName = widgetName;
                    sw.type = WidgetType.SliderWidget;
                    sw.slider = slider;

                    widgets[widgetName] = sw;
#if UNITY_EDITOR
                    debugWidgets?.Add(sw);
#endif
                    continue;
                }
            }
        }
    }

    protected override void OnSubmit()
    {
        // 메인에서 아무것도 선택 안 된 상태라면 자기 자신 닫기
        if (selectedButton == null)
        {
            UIManager.instance.Hide(gameObject.name);
            return;
        }

        string name = selectedButton.name.Replace("Button", "");

        switch (name)
        {
            case "Return":
                {
                    UIManager.instance.Hide(gameObject.name);
                    return;
                }

            default:
                {
                    Widget w;
                    if (!widgets.TryGetValue(name, out w))
                    {
                        return;
                    }

                    // Proxy를 UI 스택에 올리고 위젯 바인딩
                    UIManager.instance.Show("WidgetProxy");
                    var proxyGO = UIManager.instance.TryGetUI("WidgetProxy");
                    if (proxyGO != null)
                    {
                        var proxy = proxyGO.GetComponent<WidgetProxy>();
                        if (proxy != null)
                        {
                            proxy.Bind(w);
                        }
                    }
                    return;
                }
        }
    }
}
