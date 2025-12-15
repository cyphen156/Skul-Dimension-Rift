using Assets.Scripts.Data;

namespace Assets.Scripts.Common
{
    public static class DomainIdentityUtility
    {
        public static uint GetObjectKey(in DomainIdentity id)
        {
            return DomainKey.WithInstance(id.staticKey, id.instanceId);
        }

        public static Domain GetDomain(in DomainIdentity id)
        {
            return DomainKey.GetDomain(id.staticKey);
        }
    }
}
