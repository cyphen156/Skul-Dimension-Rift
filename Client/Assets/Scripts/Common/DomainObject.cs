using Assets.Scripts.Interface;
using UnityEngine;

namespace Assets.Scripts.Common
{
    public sealed class DomainObject : MonoBehaviour, IPoolable
    {
        [SerializeField] private DomainIdentity identity;
        [SerializeField] private bool acquired;
        [SerializeField] private bool releasing;

        #region Unity Methods
        private void OnDisable()
        {
            if (Application.isPlaying == false)
            {
                return;
            }

            if (acquired == false)
            {
                return;
            }

            if (releasing == true)
            {
                ClearInstance();
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
            if (Application.isPlaying == false)
            {
                return;
            }

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
        #endregion

        #region Identity Accessors
        public ref readonly DomainIdentity GetIdentity()
        {
            return ref identity;
        }

        public uint GetStaticKey()
        {
            return identity.staticKey;
        }

        public bool IsAcquired()
        {
            return acquired;
        }
        #endregion

        #region Identity Mutators
        public void SetStaticKey(uint staticKey)
        {
            if (acquired == true)
            {
#if UNITY_EDITOR
                Debug.LogError(
                    "[Pool Contract Violation] SetStaticKey while acquired.\n" +
                    "name=" + name +
                    " staticKey=" + DomainKey.ToHex8(identity.staticKey),
                    this
                );
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                return;
            }

            identity.staticKey = DomainKey.GetStaticId(staticKey);
        }

        public void ClearInstance()
        {
            identity.instanceId = 0;
            identity.scopeId = 0;

            acquired = false;
            releasing = false;
        }
        #endregion

        #region IPoolable
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
        #endregion
    }
}