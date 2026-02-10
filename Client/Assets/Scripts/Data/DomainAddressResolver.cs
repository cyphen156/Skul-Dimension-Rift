using Assets.Scripts.Content;
using Assets.Scripts.Interface;
using System.Collections.Generic;

namespace Assets.Scripts.Data
{
    public struct ResolvedData
    { 
        ContentHeader header;
        string LocalMetaPath;
        string RemoteMetaUri;
    }

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

        public bool Register(uint staticKey, string address)
        {
            if (map.ContainsKey(staticKey))
            {
                return false;
            }
            map[staticKey] = address;
            return true;
        }

        public bool TryResolve(uint input, out string output)
        {
            return map.TryGetValue(input, out output);
        }

        public void Unregister(uint input)
        {
            map.Remove(input);
        }
    }
}
