namespace Assets.Scripts.Content
{
    public enum VerifyResult
    {
        UpToDate,
        Outdated,
        Failed
    }

    public sealed class VerifyState
    {
        public VerifyResult result = VerifyResult.Failed;
        public ContentMeta remoteMeta = null;

        public void Clear()
        {
            result = VerifyResult.Failed;
            remoteMeta = null;
        }
    }
}
