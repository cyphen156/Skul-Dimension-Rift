using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Common
{
    public sealed class Pool
    {
        private readonly Queue<GameObject> objects;
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly uint staticKey;

        public Pool(uint staticKey, GameObject prefab, int size, Transform parent)
        {
            this.staticKey = DomainKey.GetStaticId(staticKey);
            this.prefab = prefab;
            this.parent = parent;

            objects = new Queue<GameObject>(size);

            for (int i = 0; i < size; i++)
            {
                GameObject go = CreateInstance();
                objects.Enqueue(go);
            }
        }

        private GameObject CreateInstance()
        {
            GameObject go = Object.Instantiate(prefab, parent);

            DomainObject domain = go.GetComponent<DomainObject>();
            if (domain == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[Pool Contract Violation] Prefab missing DomainObject.", prefab);
                UnityEditor.EditorApplication.isPlaying = false;
#endif
                return go;
            }

            domain.SetStaticKey(staticKey);
            go.SetActive(false);
            return go;
        }

        public GameObject Acquire()
        {
            GameObject go = objects.Count > 0 ? objects.Dequeue() : CreateInstance();
            go.SetActive(false);
            return go;
        }

        public void Release(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            go.transform.SetParent(parent, false);
            objects.Enqueue(go);
        }

        public void DestroyAll()
        {
            while (objects.Count > 0)
            {
                Object.Destroy(objects.Dequeue());
            }
        }
    }
}