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
        public static List<SerializableKeyValuePair> ToDebugList(Dictionary<string, string> map)
        {
            List<SerializableKeyValuePair> result = new List<SerializableKeyValuePair>();

            foreach (KeyValuePair<string, string> pair in map)
            {
                SerializableKeyValuePair entry = new SerializableKeyValuePair(pair.Key, pair.Value);
                result.Add(entry);
            }

            return result;
        }
    }
}
