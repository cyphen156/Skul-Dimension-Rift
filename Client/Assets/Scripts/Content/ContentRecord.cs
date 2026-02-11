using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Assets.Scripts.Content
{
    public enum ContentCategory
    {
        None = 0,
        Data = 1,
        Meta = 2,
        Bundle = 3,
    }

    [Serializable]
    public struct ContentHeader
    {
        [JsonConverter(typeof(JsonHexUInt32Converter))]
        public uint staticKey;

        public string id;
        public string schema;
        public ContentCategory category;
        public int version;

        public ContentHeader(uint staticKey, string id, string schema, ContentCategory category, int version)
        {
            this.staticKey = staticKey;
            this.id = id;
            this.schema = schema;
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
