using Assets.Scripts.Interface;
using UnityEngine;

public static class Spawner
{
    public static GameObject Spawn(uint objectKey, Vector3 position, int scopeId)
    {
        if (PoolManager.instance == null)
        {
            Debug.LogError("[Spawner] PoolManager is null.");
            return null;
        }

        uint staticKey = DomainKey.GetStaticId(objectKey);
        return PoolManager.instance.Spawn(staticKey, position, scopeId);
    }

    public static void Despawn(GameObject go)
    {
        if (PoolManager.instance == null)
        {
            Debug.LogError("[Spawner] PoolManager is null.");
            return;
        }

        PoolManager.instance.Despawn(go);
    }
}