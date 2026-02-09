using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Assets.Scripts.Content
{
    public enum Catagory
    {
        Data = 0,
        Meta = 1,
    }

    [Serializable]
    public struct ContentHeader
    {
        [JsonConverter(typeof(JsonHexUInt32Converter))]
        public uint staticKey;

        public string id;
        public Catagory category;
        public int version;

        public ContentHeader(Catagory category, uint staticKey, string id, int version)
        {
            this.staticKey = staticKey;
            this.id = id;
            this.category = category;
            this.version = version;
        }
    }

    public sealed class ContentRecord
    {
        public ContentHeader header;
        public JsonElement body;
    }
}
