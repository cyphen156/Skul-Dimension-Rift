using Assets.Scripts.Data;
using System;

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
        public string staticKey;
        public string id;
        public Catagory category;
        public int version;

        public ContentHeader(Catagory category, uint staticKey, string id, int version)
        {
            this.staticKey = DomainKeyParser.ToHex(staticKey);
            this.id = id;
            this.category = category;
            this.version = version;
        }
    }

    public sealed class ContentRecord
    {
        public ContentHeader header;
        public string body;
    }
}
