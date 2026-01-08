using Assets.Scripts.Interface;
using System.Collections.Generic;

namespace Assets.Scripts.Data
{
    public sealed class DomainAddressResolver : IResolver<uint, string>
    {
        private readonly Dictionary<Domain, string> domainMap = new Dictionary<Domain, string>();
        private readonly Dictionary<uint, string> map = new Dictionary<uint, string>();

        public Dictionary<Domain, string> DomainMap
        { 
            get 
            { 
                return domainMap; 
            } 
        }

        public IReadOnlyDictionary<uint, string> Map
        {
            get
            {
                return map;
            }
        }

        public void RegisterDomain(Domain domain, string DomainName)
        {
            domainMap.Add(domain, DomainName);
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
