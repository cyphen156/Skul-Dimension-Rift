using System;

namespace Assets.Scripts.Content
{
    public enum ContentVerifyResult
    {
        UpToDate,
        Outdated,
        Failed
    }

    [Serializable]
    public sealed class ContentManifestMeta
    {
        public int version;
        public string sha256;
    }
}
