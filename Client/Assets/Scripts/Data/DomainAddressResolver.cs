using Assets.Scripts.Interface;

namespace Assets.Scripts.Data
{
    public sealed class DomainAddressResolver : IResolver<uint, string>
    {
        public bool TryResolve(uint input, out string output)
        {
            output = string.Empty;

            if (ResourceManager.instance == null)
            {
                return false;
            }

            //if (!ResourceManager.instance.TryGetAsset<ResolveMap>(input, out ResolveMap asset))
            //{
            //    return false;
            //}

            //if (asset == null)
            //{
            //    return false;
            //}

            return string.IsNullOrEmpty(output) == false;
        }
    }
}
