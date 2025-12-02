using Assets.Scripts.Common;
using Assets.Scripts.Data;
using Assets.Scripts.Item;
using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;

[Serializable]
public class SpawnPoolConfig
{
    public ObjectDomain domain;
    public PoolKey poolKey;
    public GameObject prefab;
    public int initialSize = 30;
}

public class ObjectSpawner : MonoBehaviour
{
    public static ObjectSpawner instance;

    [SerializeField]
    private List<SpawnPoolConfig> configs = new List<SpawnPoolConfig>();

    private readonly Dictionary<ObjectDomain, SpawnPoolConfig> configMap =
        new Dictionary<ObjectDomain, SpawnPoolConfig>();

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

        InitializePools();
    }

    private void InitializePools()
    {
        if (PoolManager.instance == null)
        {
            Debug.LogError("[ObjectSpawner] PoolManager is null");
            return;
        }

        configMap.Clear();

        for (int i = 0; i < configs.Count; i++)
        {
            SpawnPoolConfig cfg = configs[i];

            if (cfg == null)
            {
                continue;
            }

            if (cfg.prefab == null)
            {
                Debug.LogWarning("[ObjectSpawner] Prefab is null for domain : " + cfg.domain);
                continue;
            }

            if (configMap.ContainsKey(cfg.domain) == true)
            {
                Debug.LogWarning("[ObjectSpawner] Duplicate domain config : " + cfg.domain);
                continue;
            }

            configMap[cfg.domain] = cfg;

            PoolManager.instance.RegisterPool(
                cfg.poolKey,
                cfg.prefab,
                cfg.initialSize,
                transform
            );
        }
    }

    public ViewObject Spawn(uint objectKey, Vector3 position)
    {
        if (PoolManager.instance == null)
        {
            Debug.LogError("[ObjectSpawner] PoolManager is null");
            return null;
        }

        ObjectDomain domain = ObjectKey.GetDomain(objectKey);

        SpawnPoolConfig cfg;
        if (!configMap.TryGetValue(domain, out cfg))
        {
            Debug.LogError("[ObjectSpawner] No config for domain : " + domain);
            return null;
        }

        GameObject go = PoolManager.instance.Spawn(cfg.poolKey, position);

        if (go == null)
        {
            return null;
        }

        ViewObject obj = go.GetComponent<ViewObject>();

        if (obj != null)
        {
            obj.SetObjectKey(objectKey);
        }

        return obj;
    }

    public T Spawn<T>(uint objectKey, Vector3 position) where T : ViewObject
    {
        ViewObject obj = Spawn(objectKey, position);

        if (obj == null)
        {
            return null;
        }

        T typed = obj as T;
        return typed;
    }

#if UNITY_EDITOR
    [Header("DEBUG")]
    [SerializeField]
    private bool debugSpawnOnPlay = true;

    [SerializeField]
    private float debugItemSpacing = 1.5f;

    [Header("Decimal Key")]
    [SerializeField]
    private static readonly uint[] debugItemKeys =
    {
        // Item / Weapon / Tier1 / Class0 / Instance0
        ObjectKey.Make(
            ObjectDomain.Item,
            (byte)ItemRole.Weapon,
            (byte)Grade.Tier1,
            0x0,
            0
        ),

        // Item / Weapon / Tier2 / Class1 / Instance0
        ObjectKey.Make(
            ObjectDomain.Item,
            (byte)ItemRole.Weapon,
            (byte)Grade.Tier2,
            0x1,
            0
        ),

        // Item / Skull / Tier1 / Class0 / Instance0
        ObjectKey.Make(
            ObjectDomain.Item,
            (byte)ItemRole.Skull,
            (byte)Grade.Tier1,
            0x0,
            0
        ),

    };
    public void Spawn()
    {
        if (debugSpawnOnPlay)
        {
            foreach (var key in debugItemKeys)
            {
                int index = Array.IndexOf(debugItemKeys, key);
                Vector3 position = new Vector3(index * debugItemSpacing, 0f, 0f);
                Spawn<ItemView>(key, position);
            }
        }
    }
#endif
}
