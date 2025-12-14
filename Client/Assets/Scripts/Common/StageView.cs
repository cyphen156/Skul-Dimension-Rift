using Assets.Scripts.Interface;
using UnityEngine;

namespace Assets.Scripts.Common
{
    public class StageView : MonoBehaviour, IPoolable
    {
        [SerializeField] private DomainIdentity identity;

        [SerializeField] private bool acquired;
        [SerializeField] private bool releasing;

        private void OnDisable()
        {
            if (!acquired)
            {
                return;
            }

            if (!releasing)
            {
                DomainIdentityUtility.ClearDomain(ref identity);
                acquired = false;
                releasing = false;
                return;
            }
        }

        void IPoolable.OnSpawned(uint staticId, ushort instanceId, int scopeId)
        {
            DomainIdentityUtility.SetDomain(ref identity, staticId, instanceId, scopeId);

            acquired = true;
            releasing = false;

            OnSpawned();
        }

        void IPoolable.OnDespawned()
        {
            releasing = true;
            OnDespawned();
        }

        private void OnSpawned()
        {
        }

        private void OnDespawned()
        {

        }
    }
}
