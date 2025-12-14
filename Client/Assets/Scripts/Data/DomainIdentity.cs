using System;

namespace Assets.Scripts.Common
{
    [Serializable]
    public struct DomainIdentity
    {
        [HexView] public uint staticKey;
        public ushort instanceId;
        public int scopeId;
    }
}
