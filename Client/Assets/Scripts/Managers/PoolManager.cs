using System.Collections.Generic;
using UnityEngine;

public sealed class PoolManager : MonoBehaviour
{
    public static PoolManager instance;

    private readonly Dictionary<uint, Pool> pools = new Dictionary<uint, Pool>();
    private ushort nextInstanceId;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
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

        ushort instanceId = nextInstanceId++;
        GameObject go = pool.Get(instanceId, scopeId);

        if (go != null)
        {
            go.transform.position = position;
        }

        return go;
    }

    public void Despawn(uint staticKey, GameObject go)
    {
        Pool pool;

        if (pools.TryGetValue(staticKey, out pool) == false)
        {
            Debug.LogError("[PoolManager] Pool not registered: " + DomainKey.ToHex8(staticKey));
            return;
        }

        pool.Return(go);
    }
}
