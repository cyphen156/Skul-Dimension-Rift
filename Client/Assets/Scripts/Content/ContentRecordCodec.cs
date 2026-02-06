namespace Assets.Scripts.Content
{
    public enum ContentClassType
    {
        None = 0,
        ContentManifest = 1,
        ContentMeta = 2,
        ContentCatalog = 3,
        SceneTable = 4
    }

    /// <summary>
    /// 콘텐츠 레코드를 각 타입에 맞게 직렬화/역직렬화 해주는 코덱
    /// 포맷 타입에 따라 바이너리 <-> 텍스트를 오가기 때문에 코덱으로 결정
    /// </summary>
    public static class ContentRecordCodec
    {
        public static bool Encode<T>()
        {

        }

        public static bool TryDecode(ContentRecord<string> record, out object body)
        {
            body = (Types)record.body;
            return true;
        }

        public static bool TryDecode(ContentRecord<byte[]> record, out object body)
        {
            body = record.header;
            return true;
        }
        private 
    }
}
