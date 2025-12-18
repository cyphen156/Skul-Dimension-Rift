using Assets.Scripts.Interface;
using System.Collections.Generic;
using UnityEngine;
using static Types;

public class Options : InteractiveUIBehaviour, IContainerEventHandler
{
    private readonly Dictionary<string, UIWidgetContainer> widgets = new Dictionary<string, UIWidgetContainer>();
    private bool isInited;

#if UNITY_EDITOR
    [SerializeField] private List<UIWidgetContainer> debugWidgets = new List<UIWidgetContainer>();
#endif

    private new void Awake()
    {
        base.Awake();
        isInited = false;
    }

    private new void Start()
    {
        AllocateChildren();
        isInited = true;
    }

    private new void OnDisable()
    {
        base.OnDisable();
        // 옵션 저장은 항상 옵션창이 닫힐때만 호출한다
        if (isInited)
        {
            GameManager.instance.SaveUserData();
        }
    }
    private void AllocateChildren()
    {
        widgets.Clear();
#if UNITY_EDITOR
        debugWidgets?.Clear();
#endif

        // Option 하위 모든 Widget 컴포넌트를 수집
        var foundWidgets = ComponentRegistrar.RegisterComponentsInChildren<UIWidgetContainer>(
            transform, 0, 0, 0, true, true
        );

        foreach (var widget in foundWidgets)
        {
            if (widget == null) 
            { 
                continue; 
            }
            GameObject parent = widget.transform.parent.gameObject;
            string parentName = parent.name;

            if (parentName.EndsWith("Button"))
            {
                parentName = parentName.Replace("Button", "");
            }
            else if (parentName.EndsWith("Slider"))
            {
                parentName = parentName.Replace("Slider", "");
            }
            GameObject group = parent.transform.parent.gameObject;

            OptionDataType groupKey;
            switch (group.name.Replace("ButtonGroup", ""))
            {
                case "Graphic":
                    groupKey = OptionDataType.Graphic;
                    break;
                case "Data":
                    groupKey = OptionDataType.Data;
                    break;
                case "Audio":
                    groupKey = OptionDataType.Audio;
                    break;
                case "GamePlay":
                    groupKey = OptionDataType.GamePlay;
                    break;
                default:
                    groupKey = OptionDataType.None;
                    break;
            }

            widget.SetWidget(groupKey, parentName);

            if (!string.IsNullOrEmpty(widget.parentName))
            {
                widgets[widget.parentName] = widget;
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
    public void HandleContainerEvent(UIWidgetContainer container, ContainerEventType type)
    {
        // 옵션 위젯에서 이벤트가 발생했을 때 처리
        switch (type)
        {
            case ContainerEventType.Submit:
                {
                    GameManager.instance.ApplyUserOptionSetting(container);
                    break;
                }
            default:
                {
                    break;
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

        string name = selectedButton.name.Replace("Button", "").Replace("Slider", "");

        switch (name)
        {
            case "Return":
                {
                    UIManager.instance.Hide(gameObject.name);
                    return;
                }
            default:
                {
                    UIWidgetContainer widget;
                    if (!widgets.TryGetValue(name, out widget))
                    {
                        return;
                    }

                    //// Proxy를 UI 스택에 올리고 위젯 바인딩
                    UIManager.instance.UseProxy(widget);
                    return;
                }
        }
    }
}
