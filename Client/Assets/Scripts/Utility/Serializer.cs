using System;
using System.Collections.Generic;

namespace Assets.Scripts.Utility
{
    [Serializable]
    public struct SerializableKeyValuePair
    {
        public string key;
        public string value;

        public SerializableKeyValuePair(string key, string value)
        {
            this.key = key;
            this.value = value;
        }
    }

    public static class Serializer
    {
        public static List<SerializableKeyValuePair> ToDebugList<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> map)
        {
            List<SerializableKeyValuePair> result = new List<SerializableKeyValuePair>();

            foreach (KeyValuePair<TKey, TValue> pair in map)
            {
                string k = Formatter.ToDebugString(pair.Key);
                string v = Formatter.ToDebugString(pair.Value);

                SerializableKeyValuePair entry = new SerializableKeyValuePair(k, v);
                result.Add(entry);
            }

            return result;
        }
    }
}
