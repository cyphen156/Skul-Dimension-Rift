using Assets.Scripts.Interface;
using UnityEngine;

namespace Assets.Scripts.Common
{
    public sealed class DomainObject : MonoBehaviour, IPoolable
    {
        [SerializeField] private DomainIdentity identity;
        [SerializeField] private bool acquired;
        [SerializeField] private bool releasing;

        public DomainIdentity GetIdentity()
        {
            return identity;
        }

        public bool IsAcquired()
        {
            return acquired;
        }

        void IPoolable.OnSpawned(ushort instanceId, int scopeId)
        {
            identity.instanceId = instanceId;
            identity.scopeId = scopeId;

            acquired = true;
            releasing = false;
        }

        void IPoolable.OnDespawned()
        {
            if (acquired == false)
            {
                return;
            }

            releasing = true;
        }

        public void Clear()
        {
            identity.staticKey = 0u;
            identity.instanceId = 0;
            identity.scopeId = 0;

            acquired = false;
            releasing = false;
        }

        private void OnDisable()
        {
            if (acquired == false)
            {
                return;
            }

            if (releasing == true)
            {
                Clear();
                return;
            }

#if UNITY_EDITOR
            Debug.LogError(
                "[Pool Contract Violation] Disabled while acquired.\n" +
                "name=" + name +
                " staticKey=" + DomainKey.ToHex8(identity.staticKey),
                this
            );
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void OnDestroy()
        {
            if (acquired == false)
            {
                return;
            }
#if UNITY_EDITOR
            Debug.LogError(
                "[Pool Contract Violation] Destroyed while acquired.\n" +
                "name=" + name +
                " staticKey=" + DomainKey.ToHex8(identity.staticKey),
                this
            );
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
