using Assets.Scripts.Interface;
using UnityEngine;

public static class Spawner
{
    public static IPoolable Spawn(uint objectKey, Vector3 position, int scopeId)
    {
        if (PoolManager.instance == null)
        {
            Debug.LogError("[Spawner] PoolManager is null.");
            return null;
        }

        uint staticId = DomainKey.GetStaticId(objectKey);
        //return PoolManager.instance.Spawn(staticId, position, scopeId);
        return null;
    }

    public static void Despawn(IPoolable obj)
    {
        if (PoolManager.instance == null)
        {
            Debug.LogError("[Spawner] PoolManager is null.");
            return;
        }

        //PoolManager.instance.Despawn(obj);
    }
}