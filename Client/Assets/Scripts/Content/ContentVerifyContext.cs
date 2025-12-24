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

        // 네트워크 계층
        NetworkError,
        Http4xx,
        Http5xx,
        Timeout,

        // 내부/규약
        InvalidPath,
        InvalidResponse,
        ParseError
    }

    public sealed class ContentVerifyContext
    {
        public bool dataUpdateSucceeded = false;

        public string targetId = string.Empty;
        public string targetSchema = string.Empty;

        public VerifyResult result = VerifyResult.None;
        public VerifyFailReason failReason = VerifyFailReason.None;

        public ContentMeta remoteMeta;

        public void Clear()
        {
            targetId = string.Empty;
            targetSchema = string.Empty;

            result = VerifyResult.None;
            failReason = VerifyFailReason.None;

            remoteMeta = null;
        }
    }
}
