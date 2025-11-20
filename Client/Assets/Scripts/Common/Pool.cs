
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Common
{
    public enum PoolKey
    {
        None = 0,
        Item = 1,
        Monster = 2,
        WorldObject = 3
    }

    public class Pool
    {
        private readonly Queue<GameObject> objects;
        private readonly Transform parent;

        public Pool(GameObject prefab, int size, Transform parent)
        {
            this.parent = parent;
            objects = new Queue<GameObject>(size);

            for (int i = 0; i < size; i++)
            {
                GameObject go = GameObject.Instantiate(prefab, parent);
                go.SetActive(false);
                objects.Enqueue(go);
            }
        }

        public GameObject Get()
        {
            if (objects.Count == 0)
            {
                Debug.LogError("[Pool] Pool is empty. Check pool size design.");
                return null;
            }

            GameObject go = objects.Dequeue();
            go.SetActive(true);
            return go;
        }

        public void Return(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            go.SetActive(false);
            objects.Enqueue(go);
        }
    }
}