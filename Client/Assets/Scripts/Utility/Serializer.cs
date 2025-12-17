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
                string k = pair.Key != null ? pair.Key.ToString() : "null";
                string v = pair.Value != null ? pair.Value.ToString() : "null";

                SerializableKeyValuePair entry = new SerializableKeyValuePair(k, v);
                result.Add(entry);
            }

            return result;
        }
    }
}
