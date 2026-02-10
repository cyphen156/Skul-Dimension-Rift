namespace Assets.Scripts.Content
{
    public class ContentPolicy
    {
        public enum SyncResult
        {
            UpToDate,   // 변경 없음
            Updated,    // 로컬 파일 교체 발생
            Failed      // 검증/다운로드/커밋 실패
        }
    }
}
