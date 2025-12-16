using Assets.Scripts.Common;
using Assets.Scripts.Data;
using Assets.Scripts.Interface;
using UnityEngine;

namespace Assets.Scripts.Stage
{
    [RequireComponent(typeof(DomainObject))]
    public sealed class StageBehaviour : MonoBehaviour, IPoolable
    {
        private DomainObject domain;

        private void Awake()
        {
            domain = GetComponent<DomainObject>();
        }

        void IPoolable.OnSpawned(ushort instanceId, int scopeId)
        {
            // Config 읽고 배치 시작
        }

        void IPoolable.OnDespawned()
        {
            // 자신이 스폰한 자식들을 먼저 Despawn 요청
        }
    }
}
