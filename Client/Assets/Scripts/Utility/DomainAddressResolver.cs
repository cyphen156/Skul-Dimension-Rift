using Assets.Scripts.Interface;
using System.Collections.Generic;

namespace Assets.Scripts.Utility
{
    public sealed class DomainAddressResolver : IResolver<uint, string>
    {
        private readonly Dictionary<uint, string> map = new Dictionary<uint, string>();

        public IReadOnlyDictionary<uint, string> Map
        {
            get
            {
                return map;
            }
        }

        public void Register(uint staticKey, string address)
        {
            map[staticKey] = address;
        }

        public bool TryResolve(uint input, out string output)
        {
            return map.TryGetValue(input, out output);
        }
    }
}
