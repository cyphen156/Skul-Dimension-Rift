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
    public Domain domain;
    public PoolKey poolKey;
    public GameObject prefab;
    public int initialSize = 30;
}

public class ObjectSpawner : MonoBehaviour
{
    public static ObjectSpawner instance;

    [SerializeField]
    private List<SpawnPoolConfig> configs = new List<SpawnPoolConfig>();

    private readonly Dictionary<Domain, SpawnPoolConfig> configMap =
        new Dictionary<Domain, SpawnPoolConfig>();

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

        Domain domain = DomainKey.GetDomain(objectKey);

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
        // Item / Grade=Tier1 / Role=Weapon / Class=0x0 / Instance=0
        DomainKey.Make(
            Domain.Item,
            (byte)Grade.Grade1,
            (byte)ItemRole.Weapon,
            0x0,
            0
        ),

        // Item / Grade=Tier2 / Role=Weapon / Class=0x1 / Instance=0
        DomainKey.Make(
            Domain.Item,
            (byte)Grade.Grade2,
            (byte)ItemRole.Weapon,
            0x1,
            0
        ),

        // Item / Grade=Tier1 / Role=Skull / Class=0x0 / Instance=0
        DomainKey.Make(
            Domain.Item,
            (byte)Grade.Grade1,
            (byte)ItemRole.Skul,
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
