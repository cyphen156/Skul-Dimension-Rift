using System;
using System.Collections.Generic;

namespace Assets.Scripts.Content
{
    [Serializable]
    public sealed class ContentManifestCatalog
    {
        public string id;
        public string schema;
        public bool requiredOnBoot;
    }

    [Serializable]
    public sealed class ContentManifest
    {
        public int version = 1;

        public string verifyRoot;
        public string metaApi;

        public string id;
        public string schema;

        public List<ContentManifestCatalog> catalogs = new List<ContentManifestCatalog>();
    }
}