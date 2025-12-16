namespace Assets.Scripts.Interface
{
    public interface IResolver<Tin, Tout>
    {
        bool TryResolve(Tin input, out Tout output);
    }
}
