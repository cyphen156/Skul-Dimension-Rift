using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Assets.Scripts.Content
{
    [Serializable]
    public sealed class ContentCatalogEntry
    {
        [JsonConverter(typeof(JsonHexUInt32Converter))]
        public uint staticKey;
        public string id;
        public string schema;
        public bool requiredOnBoot;
    }

    [Serializable] 
    public sealed class SceneEntry
    {
        [JsonConverter(typeof(JsonHexUInt32Converter))]
        public uint staticKey;
        public string id;
        public string schema;
        public string ownerCatalogId;
    }

    [Serializable]
    public sealed class ContentManifest
    {
        [JsonConverter(typeof(JsonHexUInt32Converter))]
        public uint staticKey;
        public string id;
        public string schema;
        public string verifyRoot;
        public string metaApi;

        public List<ContentCatalogEntry> contentCatalogs = new List<ContentCatalogEntry>();
        public List<SceneEntry> scenes = new List<SceneEntry>();
    }
}