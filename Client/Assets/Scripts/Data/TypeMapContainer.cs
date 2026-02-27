using Assets.Scripts.Interface;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.Data
{
    public enum  AccessMode 
    {
        Public = 0,
        Protected = 1,
        Internal = 2,
        Private = 3,
    }

    [Serializable]
    internal class TypeMapContainer : IContainer
    {
        internal readonly Dictionary<Type, object> Maps = new Dictionary<Type, object>();
        internal readonly object LockObj = new object();
        internal readonly AccessMode mode;
        public AccessMode Mode => mode;

        internal TypeMapContainer(AccessMode mode)
        {
            this.mode = mode;
        }

    }
}
