namespace Assets.Scripts.Interface
{
    public interface ISubSystem
    {
        void InitializeSubSystem();
        void TickSubSystem();
        void ShutdownSubSystem();
    }
}
