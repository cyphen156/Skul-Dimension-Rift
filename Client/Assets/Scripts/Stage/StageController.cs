using Assets.Scripts.Common;
using UnityEngine;

[RequireComponent(typeof(DomainObject))]
public sealed class StageController : MonoBehaviour
{
    private DomainObject domain;

    private void Awake()
    {
        domain = GetComponent<DomainObject>();
    }

    public void OnSpawnedInternal()
    {
        DomainIdentity id = domain.GetIdentity();
    }

    public void OnDespawnedInternal()
    {
    }
}