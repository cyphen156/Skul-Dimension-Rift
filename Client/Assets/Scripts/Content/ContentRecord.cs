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
        public Catagory category;
        public string staticKey;
        public string id;
        public int version;

        public ContentHeader(Catagory category, uint staticKey, string id, int version)
        {
            this.category = category;
            this.staticKey = DomainKeyParser.ToHex(staticKey);
            this.id = id;
            this.version = version;
        }
    }

    public sealed class ContentRecord
    {
        public ContentHeader header;
        public string body;
    }
}
