using System;
using System.Collections.Generic;

namespace Assets.Scripts.Utility
{
    [Serializable]
    public struct DebugKeyValuePair
    {
        public string key;
        public string value;

        public DebugKeyValuePair(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }

    public static class DebugUtility
    {
        public static List<DebugKeyValuePair> ToDebugList<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> map)
        {
            List<DebugKeyValuePair> result = new List<DebugKeyValuePair>();

            foreach (KeyValuePair<TKey, TValue> pair in map)
            {
                string k = Formatter.ToDebugString(pair.Key);
                string v = Formatter.ToDebugString(pair.Value);

                DebugKeyValuePair entry = new DebugKeyValuePair(k, v);
                result.Add(entry);
            }

            return result;
        }
    }
}
