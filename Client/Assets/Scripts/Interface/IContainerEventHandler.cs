namespace Assets.Scripts.Interface
{
    public enum ContainerEventType
    {
        Submit,
        ValueChanged,
        Click,
        Drag,
        Custom
    }
    public interface IContainerEventHandler
    {
        void HandleContainerEvent(UIWidgetContainer container, ContainerEventType type);
    }
}
