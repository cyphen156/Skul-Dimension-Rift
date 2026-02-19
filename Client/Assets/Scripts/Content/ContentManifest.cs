using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Assets.Scripts.Content
{
    /// <summary>
    /// ContentManifest를 구성하는 항목의 기본 정보
    /// 단일 항목이지만 엔트리로서 편입되어 다른 컨텐츠와 동일하게 관리하기 위해 ContentEntry로 정의
    /// 다만 Manifest 본문의 필드에 정의하지 않고 엔트리로서 생성될 수 있도록 도와주는 역할
    /// CMS.ApplyGameIdentityAsync()에서 게임 아이덴티티 적용이 완료된 후에 ContentManifestEntry로 변환되어 Entry로서 관리됨
    /// </summary>
    [Serializable]
    public sealed class ContentManifestEntry : ContentEntry
    {
    }

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