using System;

namespace Assets.Scripts.Content
{
    public enum Catagory
    {
        Data = 0,
        Meta = 1,
    }

    public enum ContentFormat
    {
        Text = 0,
        Binary = 1,
    }
    
    [Serializable]
    public struct ContentHeader
    {
        public Catagory category;
        public string staticKey;
        public string id;
        public ContentFormat format;
        public int version;
    }

    public sealed class ContentRecord<T>
    {
        public ContentHeader header;
        public T body;
        /// 포맷에 따라 해석 방식이 달라지므로 
        /// T는 string 또는 Byte[]만을 사용하도록한다.
    }
}
