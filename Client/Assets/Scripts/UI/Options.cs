using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Options : InteractiveUIBehaviour
{
    private readonly Dictionary<string, Widget> widgets = new Dictionary<string, Widget>();

#if UNITY_EDITOR
    [SerializeField] private List<Widget> debugWidgets = new List<Widget>();
#endif

    private new void Awake()
    {
        base.Awake();
        AllocateChildren();
    }

    private void AllocateChildren()
    {
        widgets.Clear();
#if UNITY_EDITOR
        debugWidgets?.Clear();
#endif

        // Option 하위 모든 Widget 컴포넌트를 수집
        var foundWidgets = ComponentRegistrar.RegisterComponentsInChildren<Widget>(
            transform, 0, 0, 0, true, true
        );

        foreach (var widget in foundWidgets)
        {
            if (widget == null) { continue; }

            // 위젯 내부 구성 요소 연결
            switch (widget.widgetType)
            {
                case WidgetType.StepperWidget:
                    {
                        var stepper = widget.widget as StepperWidget;
                        if (stepper == null) 
                        { 
                            break; 
                        }

                        var leftT = widget.transform.Find("LeftArrowButton");
                        var rightT = widget.transform.Find("RightArrowButton");
                        var textT = widget.transform.Find("OptionText");

                        if (leftT != null) 
                        {
                            stepper.leftArrow = leftT.GetComponent<Button>(); 
                        }

                        if (rightT != null) 
                        { 
                            stepper.rightArrow = rightT.GetComponent<Button>(); 
                        }
                        if (textT != null) 
                        {
                            stepper.optionText = textT.GetComponent<TMP_Text>(); 
                        }
                        break;
                    }

                case WidgetType.SliderWidget:
                    {
                        var slider = widget.widget as SliderWidget;
                        if (slider == null) { break; }

                        slider.slider = widget.GetComponentInChildren<Slider>(true);
                        break;
                    }

                case WidgetType.OneShotWidget:
                    {
                        var oneShot = widget.widget as OneShotWidget;
                        if (oneShot == null) 
                        { 
                            break;
                        }

                        var button = widget.transform.Find("OneShotButton");
                        var text = widget.transform.Find("OptionText");

                        if (button != null) 
                        { 
                            oneShot.oneShotButton = button.GetComponent<Button>(); 
                        }
                        if (text != null) 
                        { 
                            oneShot.optionText = text.GetComponent<TMP_Text>(); 
                        }
                        break;
                    }
            }

            if (!string.IsNullOrEmpty(widget.buttonName))
            {
                widgets[widget.buttonName] = widget;
#if UNITY_EDITOR
                debugWidgets?.Add(widget);
#endif
            }
            else
            {
                // 매칭 실패 시 디버그 확인용
                Debug.LogWarning($"[Options] widgetName not set for {widget.name}");
            }
        }
    }

    //    protected override void OnSubmit()
    //    {
    //        // 메인에서 아무것도 선택 안 된 상태라면 자기 자신 닫기
    //        if (selectedButton == null)
    //        {
    //            UIManager.instance.Hide(gameObject.name);
    //            return;
    //        }

    //        string name = selectedButton.name.Replace("Button", "");

    //        switch (name)
    //        {
    //            case "Return":
    //                {
    //                    UIManager.instance.Hide(gameObject.name);
    //                    return;
    //                }

    //            default:
    //                {
    //                    Widget w;
    //                    if (!widgets.TryGetValue(name, out w))
    //                    {
    //                        return;
    //                    }

    //                    // Proxy를 UI 스택에 올리고 위젯 바인딩
    //                    UIManager.instance.Show("WidgetProxy");
    //                    var proxyGO = UIManager.instance.TryGetUI("WidgetProxy");
    //                    if (proxyGO != null)
    //                    {
    //                        var proxy = proxyGO.GetComponent<WidgetProxy>();
    //                        if (proxy != null)
    //                        {
    //                            proxy.Bind(w);
    //                        }
    //                    }
    //                    return;
    //                }
    //        }
    //    }
}
