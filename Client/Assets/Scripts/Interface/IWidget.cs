namespace Assets.Scripts.Interface
{
    public enum WidgetType
    {
        None, // set as default
        StepperWidget,
        SliderWidget,
        OneShotWidget,
        PromptWidget,
    }

    /// <summary>
    /// 위젯 오브젝트 다형성을 위한 인터페이스
    /// </summary>
    public interface IWidget
    {
        void Refresh(string data);
    }
}
