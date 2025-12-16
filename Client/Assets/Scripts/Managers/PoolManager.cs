using Assets.Scripts.Common;
using Assets.Scripts.Interface;
using System.Collections.Generic;
using UnityEngine;

public sealed class PoolManager : MonoBehaviour
{
    public static PoolManager instance;

    private readonly Dictionary<uint, Pool> pools = new Dictionary<uint, Pool>();
    private readonly List<IPoolable> tempPoolables = new List<IPoolable>();
    private ushort nextInstanceId;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            nextInstanceId = 1;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterPool(uint staticKey, GameObject prefab, int size, Transform parent)
    {
        if (pools.ContainsKey(staticKey))
        {
            return;
        }

        Pool pool = new Pool(staticKey, prefab, size, parent);
        pools.Add(staticKey, pool);
    }

    public GameObject Spawn(uint staticKey, Vector3 position, int scopeId)
    {
        Pool pool;

        if (pools.TryGetValue(staticKey, out pool) == false)
        {
            Debug.LogError("[PoolManager] Pool not registered: " + DomainKey.ToHex8(staticKey));
            return null;
        }

        ushort instanceId = AllocateInstanceId();

        if (instanceId == 0)
        {
            Debug.LogError("[PoolManager] InstanceId exhausted.");
            return null;
        }

        GameObject go = pool.Acquire();

        if (go == null)
        {
            return null;
        }

        BroadcastSpawn(go, instanceId, scopeId);

        go.transform.position = position;
        go.SetActive(true);

        return go;
    }

    public void Despawn(GameObject go)
    {
        if (go == null)
        {
            return;
        }

        DomainObject domain = go.GetComponent<DomainObject>();
        if (domain == null)
        {
            Debug.LogError("[PoolManager] Missing DomainObject.");
            return;
        }

        uint staticKey = domain.GetStaticKey();

        Pool pool;
        if (pools.TryGetValue(staticKey, out pool) == false)
        {
            Debug.LogError("[PoolManager] Pool not registered: " + DomainKey.ToHex8(staticKey));
            return;
        }

        BroadcastDespawn(go);

        go.SetActive(false);
        pool.Release(go);
    }

    public void DisposePool(uint staticKey)
    {
        Pool pool;
        if (pools.TryGetValue(staticKey, out pool) == false)
        {
            return;
        }

        pools.Remove(staticKey);
        pool.DestroyAll();
    }

    public void DisposeAllPools()
    {
        foreach (KeyValuePair<uint, Pool> kv in pools)
        {
            kv.Value.DestroyAll();
        }

        pools.Clear();
    }

    private ushort AllocateInstanceId()
    {
        ushort id = nextInstanceId;
        nextInstanceId++;

        if (nextInstanceId == 0)
        {
            nextInstanceId = 1;
        }

        return id;
    }

    private void BroadcastSpawn(GameObject go, ushort instanceId, int scopeId)
    {
        tempPoolables.Clear();
        go.GetComponents(tempPoolables);

        for (int i = 0; i < tempPoolables.Count; i++)
        {
            tempPoolables[i].OnSpawned(instanceId, scopeId);
        }
    }

    private void BroadcastDespawn(GameObject go)
    {
        tempPoolables.Clear();
        go.GetComponents(tempPoolables);

        for (int i = 0; i < tempPoolables.Count; i++)
        {
            tempPoolables[i].OnDespawned();
        }
    }
}