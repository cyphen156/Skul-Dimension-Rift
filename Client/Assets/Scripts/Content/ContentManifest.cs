using System;
using System.Collections.Generic;

namespace Assets.Scripts.Content
{
    [Serializable]
    public sealed class ContentCatalogEntry
    {
        public uint staticKey;
        public string id;
        public string schema;
        public bool requiredOnBoot;
    }

    [Serializable] 
    public sealed class SceneEntry
    {
        public uint staticKey;
        public string id;
        public string schema;
        public string ownerCatalogId;
    }

    [Serializable]
    public sealed class ContentManifest
    {
        public uint staticKey;
        public string id;
        public string schema;
        public string verifyRoot;
        public string metaApi;

        public List<ContentCatalogEntry> contentCatalogs = new List<ContentCatalogEntry>();
        public List<SceneEntry> scenes = new List<SceneEntry>();
    }
}