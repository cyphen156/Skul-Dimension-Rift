namespace Assets.Scripts.Content
{
    public enum VerifyResult
    {
        None,       // 미검증
        UpToDate,   // 최신 상태
        Outdated,   // 구버전  
        Failed      // 검증 실패
    }

    public enum VerifyFailReason
    {
        None,

        // 네트워크 계층 에러
        InvalidUri,
        NetworkError,
        Timeout,

        // 내부/규약
        Canceled,
        NotInitialized,
        NotFound,
        InvalidPath,
        InvalidResponse,
        AccessDenied,
        SaveFailed,
        ParseError
    }

    public sealed class ContentVerifyContext
    {
        public bool dataUpdateSucceeded = false;

        public string targetId = string.Empty;
        public string targetSchema = string.Empty;
        public ContentCategory category = ContentCategory.None;

        public VerifyResult result = VerifyResult.None;
        public VerifyFailReason failReason = VerifyFailReason.None;
        public long httpResponseCode = 0;

        public ContentMeta remoteMeta;

        public void Bind(string targetId, string targetSchema, ContentCategory category)
        {
            Clear();
            this.targetId = targetId;
            this.targetSchema = targetSchema;
            this.category = category;
        }

        private void Clear()
        {
            dataUpdateSucceeded = false;

            targetId = string.Empty;
            targetSchema = string.Empty;
            category = ContentCategory.None;

            result = VerifyResult.None;
            failReason = VerifyFailReason.None;
            httpResponseCode = 0;

            remoteMeta = null;
        }
    }
}
