namespace Assets.Scripts.Interface
{
    public interface IPoolable
    {
        void OnSpawned(ushort instanceId, int scopeId);
        void OnDespawned();
    }
}
