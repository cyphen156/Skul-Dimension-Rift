using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Content
{
    public enum ContentVerifyResult
    {
        UpToDate,
        Outdated,
        Failed
    }

    public sealed class ContentVerifyState
    {
        public ContentVerifyResult result = ContentVerifyResult.Failed;
        public ContentMeta remoteMeta = null;
    }

    public static class ContentVerifier
    {
        public static IEnumerator VerifyMeta(
            string remoteMetaUri,
            ContentMeta localMeta,
            ContentVerifyState verifyState
        )
        {
            if (verifyState == null)
            {
                yield break;
            }

            verifyState.result = ContentVerifyResult.Failed;
            verifyState.remoteMeta = null;

            if (string.IsNullOrEmpty(remoteMetaUri) == true)
            {
                yield break;
            }

            UnityWebRequest request = null;

            try
            {
                request = UnityWebRequest.Get(remoteMetaUri);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    yield break;
                }

                string text = request.downloadHandler.text;

                if (string.IsNullOrEmpty(text) == true)
                {
                    yield break;
                }

                ContentMeta remoteMeta = JsonUtility.FromJson<ContentMeta>(text);

                if (remoteMeta == null)
                {
                    yield break;
                }

                if (string.IsNullOrEmpty(remoteMeta.sha256) == true)
                {
                    yield break;
                }

                verifyState.remoteMeta = remoteMeta;

                if (localMeta == null)
                {
                    verifyState.result = ContentVerifyResult.Outdated;
                    yield break;
                }

                if (string.IsNullOrEmpty(localMeta.sha256) == true)
                {
                    verifyState.result = ContentVerifyResult.Outdated;
                    yield break;
                }

                if (string.Equals(localMeta.sha256, remoteMeta.sha256, System.StringComparison.Ordinal) == true)
                {
                    verifyState.result = ContentVerifyResult.UpToDate;
                    yield break;
                }

                verifyState.result = ContentVerifyResult.Outdated;
            }
            finally
            {
                if (request != null)
                {
                    request.Dispose();
                }
            }
        }
    }
}
