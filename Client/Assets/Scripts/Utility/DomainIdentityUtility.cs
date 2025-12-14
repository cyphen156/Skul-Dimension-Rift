using Assets.Scripts.Data;

namespace Assets.Scripts.Common
{
    public static class DomainIdentityUtility
    {
        public static void SetDomain(ref DomainIdentity identity, uint staticKey, ushort instanceId, int scopeId)
        {
            identity.staticKey = staticKey;
            identity.instanceId = instanceId;
            identity.scopeId = scopeId;
        }

        public static void ClearDomain(ref DomainIdentity identity)
        {
            identity.staticKey = 0u;
            identity.instanceId = 0;
            identity.scopeId = 0;
        }

        public static uint GetObjectKey(in DomainIdentity identity)
        {
            return DomainKey.WithInstance(identity.staticKey, identity.instanceId);
        }

        public static Domain GetDomain(in DomainIdentity identity)
        {
            return DomainKey.GetDomain(identity.staticKey);
        }
    }
}
