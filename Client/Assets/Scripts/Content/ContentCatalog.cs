using System;
using System.Collections.Generic;

namespace Assets.Scripts.Content
{
    public enum CatalogState
    {
        None,
        InProgress,
        Ready,
        Failed
    }
    /// <summary>
    /// 실제 제공되는 콘텐츠 번들 항목 정보
    /// 항상 카탈로그를 통해 최신인지 판단되었다고 가정함
    /// </summary>
    [Serializable]
    public sealed class ContentBundleEntry
    {
        public string id;
        public string dataUri;
        public string sha256;
        public long sizeBytes;
    }

    /// <summary>
    /// 콘텐츠 팩에 필요한 데이터셋 항목 정보
    /// Ex) StageDataSet, CharacterDataSet 등
    /// 데이터 셋의 경우 구현 방식이 다양할 수 있으므로 
    /// 각 데이터셋 항목에 대한 해석 방식을 별도로 정의해야 함
    /// </summary>
    [Serializable]
    public sealed class ContentDataSetEntry
    {
        public string id;
        public string dataUri;
    }

    /// <summary>
    /// 콘텐츠 카탈로그 정보
    /// 매니페스트에 정의된 
    /// 콘텐츠 카탈로그 항목에 대응되는 
    /// 실제 콘텐츠 번들 및 데이터셋 정보
    /// 1. 콘텐츠 번들 목록 (정적 경로를 통한 제공)
    /// 2. 데이터셋 목록 (서버 권위 데이터 제공) 
    ///  -> 런타임에 동적으로 갱신 가능
    ///  if 네트워크 실패시 
    ///     --> 기존 로컬 데이터셋 사용
    /// </summary>
    [Serializable]
    public sealed class ContentCatalog
    {
        public int version = 1;

        public string id;
        public string schema;

        public List<ContentBundleEntry> bundles = new List<ContentBundleEntry>();
        public List<ContentDataSetEntry> dataSets = new List<ContentDataSetEntry>();
    }
}