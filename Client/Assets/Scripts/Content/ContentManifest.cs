using System;
using System.Collections.Generic;

namespace Assets.Scripts.Content
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
        public string metaApi;
    }

    [Serializable]
    public sealed class ContentManifest
    {
        public int version = 1;

        public string debugUri;
        public string serverRoot;
        public string basePath; 
        
        public ContentVerifyInfo verify;
        public List<ContentManifestCatalog> catalogs = new List<ContentManifestCatalog>();
    }
}
