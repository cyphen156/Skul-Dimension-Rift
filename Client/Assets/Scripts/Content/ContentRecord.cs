using System;

namespace Assets.Scripts.Content
{
    [Serializable]
    public struct ContentHeader
    {
        public int version;
        public uint staticKey;
        public string schema;
        public string format;
    }

    public interface IContentBody
    {
    }
    
    public class ContentRecord
    {
        public ContentHeader header;
        public string body;
    }
}
