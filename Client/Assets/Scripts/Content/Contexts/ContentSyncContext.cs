namespace Assets.Scripts.Content
{
    public enum SyncResult
    {
        None,       // 동기화판단 이전
        UpToDate,   // 변경 없음
        Updated,    // 로컬 파일 교체 발생
        Failed      // 검증/다운로드/커밋 실패
    }

    public enum VerifyResult
    {
        None,       // 미검증
        UpToDate,   // 최신 상태
        Outdated,   // 구버전  
        Failed      // 검증 실패
    }

    public enum SyncFailReason
    {
        None,

        // 검증 실패
        TypeMismatch,
        HashMismatch,

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
        ParseError,

        // 원인 불명
        Unknown
    }

    public sealed class ContentSyncContext
    {
        public uint staticKey = 0u;

        public bool dataUpdateSucceeded = false;

        public string targetId = string.Empty;
        public string targetSchema = string.Empty;
        public ContentCategory category = ContentCategory.None;

        public SyncResult syncResult = SyncResult.None;
        public VerifyResult verifyResult = VerifyResult.None;
        public SyncFailReason failReason = SyncFailReason.None;
        public long httpResponseCode = 0;

        public ContentMeta remoteMeta;

        public void Bind(uint staticKey, string targetId, string targetSchema, ContentCategory category)
        {
            Clear();
            this.staticKey = staticKey;
            this.targetId = targetId;
            this.targetSchema = targetSchema;
            this.category = category;
        }

        private void Clear()
        {
            staticKey = 0u; 

            dataUpdateSucceeded = false;

            targetId = string.Empty;
            targetSchema = string.Empty;
            category = ContentCategory.None;

            syncResult = SyncResult.None;
            verifyResult = VerifyResult.None;
            failReason = SyncFailReason.None;
            httpResponseCode = 0;

            remoteMeta = null;
        }
    }
}
