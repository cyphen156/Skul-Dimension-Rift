using Assets.Scripts.Common;
using Assets.Scripts.Data;

namespace Assets.Scripts.Utility
{
    public static class DomainIdentityUtility
    {
        public static uint GetObjectKey(DomainObject obj)
        {
            ref readonly DomainIdentity id = ref obj.GetIdentity();
            return DomainKey.WithInstance(id.staticKey, id.instanceId);
        }
        
        public static uint GetObjectKey(in DomainIdentity id)
        {
            return DomainKey.WithInstance(id.staticKey, id.instanceId);
        }

        public static Domain GetDomain(DomainObject obj)
        {
            ref readonly DomainIdentity id = ref obj.GetIdentity();
            return DomainKey.GetDomain(id.staticKey);
        }
        
        public static Domain GetDomain(in DomainIdentity id)
        {
            return DomainKey.GetDomain(id.staticKey);
        }
    }
}
