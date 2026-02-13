using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Assets.Scripts.Content
{
    [Serializable]
    public sealed class ContentCatalogEntry : ContentEntry
    {
        public bool requiredOnBoot;
    }

    [Serializable]
    public sealed class SceneEntry : ContentEntry
    {
        [JsonConverter(typeof(JsonHexUInt32Converter))]
        public uint ownerStaticKey;
    }

    [Serializable]
    public sealed class ContentManifest
    {
        [JsonConverter(typeof(JsonHexUInt32Converter))]
        public uint staticKey;
        public string verifyRoot;
        public string metaApi;

        public List<ContentCatalogEntry> contentCatalogs = new List<ContentCatalogEntry>();
        public List<SceneEntry> scenes = new List<SceneEntry>();
    }
}