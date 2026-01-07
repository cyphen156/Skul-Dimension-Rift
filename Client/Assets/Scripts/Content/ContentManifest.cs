using System;
using System.Collections.Generic;

namespace Assets.Scripts.Content
{
    [Serializable]
    public sealed class ContentCatalogEntry
    {
        public string id;
        public string schema;
        public bool requiredOnBoot;
    }

    [Serializable] 
    public sealed class SceneEntry
    {
        public string sceneName;
        public string staticKey;
        public string ownerCatalogId;
    }

    [Serializable]
    public sealed class ContentManifest
    {
        public int version = 1;

        public string verifyRoot;
        public string metaApi;

        public string id;
        public string schema;

        public List<ContentCatalogEntry> contentCatalogs = new List<ContentCatalogEntry>();
        public List<SceneEntry> scenes = new List<SceneEntry>();
    }
}