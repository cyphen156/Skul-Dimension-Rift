using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Common
{
    [Serializable]
    public class Locker<T>
    {
        [Serializable]
        public class Entry
        {
            [SerializeField] private T target;
            [SerializeField] private bool hasTimer;
            [SerializeField] private float unlockTime;

            public T Target
            {
                get
                {
                    return target;
                }
            }

            public bool HasTimer
            {
                get
                {
                    return hasTimer;
                }
            }

            public float UnlockTime
            {
                get
                {
                    return unlockTime;
                }
            }

            public Entry(T target, float duration)
            {
                this.target = target;

                if (duration > 0.0f)
                {
                    hasTimer = true;
                    unlockTime = Time.time + duration;
                }
                else
                {
                    hasTimer = false;
                    unlockTime = 0.0f;
                }
            }

            public bool IsLocked()
            {
                if (hasTimer == false)
                {
                    return true;
                }

                if (Time.time < unlockTime)
                {
                    return true;
                }

                return false;
            }
        }

        [SerializeField]
        private List<Entry> entries;

        public Locker()
        {
            entries = new List<Entry>();
        }

        private int FindIndex(T target)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];

                if (EqualityComparer<T>.Default.Equals(entry.Target, target) == true)
                {
                    return i;
                }
            }

            return -1;
        }

        public void Lock(T target, float duration = 0.0f)
        {
            int index = FindIndex(target);

            if (index >= 0)
            {
                entries[index] = new Entry(target, duration);
                return;
            }

            Entry newEntry = new Entry(target, duration);
            entries.Add(newEntry);
        }

        private void Unlock(T target)
        {
            int index = FindIndex(target);

            if (index < 0)
            {
                return;
            }

            entries.RemoveAt(index);
        }

        public void ForceUnlock(T target)
        {
            int index = FindIndex(target);
            if (index < 0)
            {
                return;
            }

            entries.RemoveAt(index);

        }

        public bool IsLocked(T target)
        {
            int index = FindIndex(target);

            if (index < 0)
            {
                return false;
            }

            Entry entry = entries[index];
            bool locked = entry.IsLocked();

            if (locked == false)
            {
                Unlock(target);
            }

            return locked;
        }
    }
}
