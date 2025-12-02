using Assets.Scripts.Common;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager instance;

    private readonly Dictionary<PoolKey, Pool> pools = new Dictionary<PoolKey, Pool>();
#if UNITY_EDITOR
    [SerializeField] List<PoolKey> debugRegisteredPools = new List<PoolKey>();
    private void Update()
    {
        debugRegisteredPools.Clear();
        foreach (var key in pools.Keys)
        {
            debugRegisteredPools.Add(key);
        }
    }
#endif
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
            return;
        }
    }

    public void RegisterPool(PoolKey key, GameObject prefab, int size, Transform parent)
    {
        if (prefab == null)
        {
            Debug.LogError("[PoolManager] Prefab is null for key: " + key);
            return;
        }

        if (pools.ContainsKey(key) == true)
        {
            return;
        }

        Pool pool = new Pool(prefab, size, parent);
        pools.Add(key, pool);
    }

    public GameObject Spawn(PoolKey key, Vector3 position)
    {
        Pool pool;
        if (!pools.TryGetValue(key, out pool))
        {
            Debug.LogError("[PoolManager] Pool not registered : " + key);
            return null;
        }

        GameObject go = pool.Get();
        if (go != null)
        {
            Transform t = go.transform;
            t.position = position;
        }

        return go;
    }

    public void Despawn(PoolKey key, GameObject go)
    {
        if (go == null)
        {
            return;
        }

        Pool pool;
        if (!pools.TryGetValue(key, out pool))
        {
            Debug.LogError("[PoolManager] Pool not registered : " + key);
            return;
        }

        pool.Return(go);
    }
}
