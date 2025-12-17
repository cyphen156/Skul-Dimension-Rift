using System;
using System.Collections.Generic;

namespace Assets.Scripts.Data
{
    [Serializable]
    public sealed class ContentManifestCatalog
    {
        public string id;
        public string catalogPath;
    }

    [Serializable]
    public class ContentVerifyInfo
    {
        public string manifestMetaPath;
    }
    
    public class ContentManifest
    {
        public int version = 1;
        public string debugUri;
        public string baseUri;
        public ContentVerifyInfo verify;
        public List<ContentManifestCatalog> catalogs = new List<ContentManifestCatalog>();
    }
}
