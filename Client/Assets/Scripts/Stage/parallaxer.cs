using Assets.Scripts.Common;
using Assets.Scripts.Interface;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Stage
{
    public class parallaxer : MonoBehaviour
    {
        [SerializeField] private Transform followTransform;
        [SerializeField] private List<Transform> followerList = new List<Transform>();
        [SerializeField] private float weight = 1.0f;

        private void Awake()
        {
            followerList = ComponentRegistrar.RegisterComponentsInChildren<Transform>(transform, 0, 2, 3, includeInactive: true, continuous: true);

            if (followerList.Contains(transform) == true)
            {
                followerList.Remove(transform);
            }

            int size = followerList.Count;

            if (size != 0)
            {
                weight = weight / size;
                foreach (Transform t in followerList)
                {
                    if (t.gameObject.GetComponent<IMoveable>() == null)
                    {
                        //t.gameObject.AddComponent<Movement>();
                    }
                }
            }
        }

        public void SetFollow(Transform target)
        {
            followTransform = target;
        }
    }
}