using System;

namespace Assets.Scripts.Content
{
    [Serializable]
    public sealed class ContentMeta
    {
        public int version;
        public string sha256;
        public string dataUri;
        public long size;

        public void Clear()
        {
            version = 0;
            sha256 = null;
            dataUri = null;
            size = 0u;
        }
    }
}
