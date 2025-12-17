using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Content
{
    public static class ContentVerifier
    {
        public static IEnumerator VerifyManifest(
            string remoteMetaUri,
            ContentManifestMeta localMeta,
            Action<ContentVerifyResult, ContentManifestMeta> onComplete
        )
        {
            UnityWebRequest request = UnityWebRequest.Get(remoteMetaUri);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete(ContentVerifyResult.Failed, null);
                yield break;
            }

            ContentManifestMeta remoteMeta =
                JsonUtility.FromJson<ContentManifestMeta>(request.downloadHandler.text);

            if (remoteMeta == null)
            {
                onComplete(ContentVerifyResult.Failed, null);
                yield break;
            }

            if (string.IsNullOrEmpty(remoteMeta.sha256) == true)
            {
                onComplete(ContentVerifyResult.Failed, null);
                yield break;
            }

            if (localMeta == null)
            {
                onComplete(ContentVerifyResult.Outdated, remoteMeta);
                yield break;
            }

            if (string.IsNullOrEmpty(localMeta.sha256) == true)
            {
                onComplete(ContentVerifyResult.Outdated, remoteMeta);
                yield break;
            }

            if (localMeta.sha256 == remoteMeta.sha256)
            {
                onComplete(ContentVerifyResult.UpToDate, remoteMeta);
                yield break;
            }

            onComplete(ContentVerifyResult.Outdated, remoteMeta);
        }
    }
}
