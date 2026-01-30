using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

/// <summary>
/// 컨텐츠 관련 정책을 관리, 제공하는 시스템
/// </summary>
public static class ContentManagementSystem
{
    private static void UpdateContentCatalog(List<IResourceLocator> registers, List<IResourceLocator> unregisters)
    {
        foreach (IResourceLocator locator in registers)
        {
            Addressables.AddResourceLocator(locator);
        }

        foreach (IResourceLocator locator in unregisters)
        {
            Addressables.RemoveResourceLocator(locator);
        }
    }
    //public static 
    //    public static IEnumerator CheckContentConsistency(ContentVerifyContext result)
    //    {
    //        if (result == null)
    //        {
    //            yield break;
    //        }

    //        // 1) Manifest meta 인증

    //        ContentManifest manifest = ResourceManager.instance.ContentManifest;
    //        ContentMeta manifestMeta;
    //        ResourceManager.instance.LoadContentMeta(out manifestMeta, manifest.id, manifest.schema);

    //        yield return VerifyContentMeta(manifestMeta, manifest.id, manifest.schema, result);

    //        if (result.result != VerifyResult.UpToDate)
    //        {
    //            yield break;
    //        }

    //        // 2) Manifest payload hash -> meta 검증
    //        {
    //            string manifestPayloadPath = Path.Combine(
    //                Application.persistentDataPath,
    //                "Data",
    //                manifest.schema,
    //                manifest.id + ".json"
    //            );

    //            StreamContainer manifestStream = ResourceManager.instance.GetStreamContainer(manifestPayloadPath);

    //            //if (hashCts != null)
    //            //{
    //            //    hashCts.Cancel();
    //            //    hashCts.Dispose();
    //            //    hashCts = null;
    //            //}

    //            //hashCts = new CancellationTokenSource();

    //            //Task<string> t = Sha256StreamTask.ComputeHexAsync(
    //            //    manifestPayloadPath,
    //            //    256 * 1024,
    //            //    hashCts.Token
    //            //);

    //            //while (t.IsCompleted == false)
    //            //{
    //            //    yield return null;
    //            //}

    //            //string payloadHex = t.Result;

    //            //if (string.Equals(payloadHex, result.remoteMeta.sha256, StringComparison.OrdinalIgnoreCase) == false)
    //            //{
    //            //    result.result = VerifyResult.Failed;
    //            //    result.failReason = VerifyFailReason.InvalidResponse;
    //            //    yield break;
    //            //}
    //        }

    //        // 3) Catalog 반복
    //        for (int i = 0; i < contentManifest.contentCatalogs.Count; i++)
    //        {
    //            ContentCatalogEntry entry = contentManifest.contentCatalogs[i];
    //            if (entry == null)
    //            {
    //                continue;
    //            }

    //            ContentVerifyContext ctx = new ContentVerifyContext();

    //            // 3-1) Catalog meta 인증
    //            ContentMeta catalogMeta;
    //            LoadContentMeta(out catalogMeta, entry.id, entry.schema);

    //            yield return VerifyContentMeta(
    //                catalogMeta,
    //                entry.id,
    //                entry.schema,
    //                ctx
    //            );

    //            if (ctx.result != VerifyResult.UpToDate)
    //            {
    //                result.result = ctx.result;
    //                result.failReason = ctx.failReason;
    //                yield break;
    //            }

    //            // 3-2) Catalog payload hash -> meta 검증
    //            {
    //                string catalogPayloadPath = Path.Combine(
    //                    Application.persistentDataPath,
    //                    "Data",
    //                    entry.schema,
    //                    entry.id + ".json"
    //                );

    //                //if (hashCts != null)
    //                //{
    //                //    hashCts.Cancel();
    //                //    hashCts.Dispose();
    //                //    hashCts = null;
    //                //}

    //                //hashCts = new CancellationTokenSource();

    //                //Task<string> t = Sha256StreamTask.ComputeFileHexAsync(
    //                //    catalogPayloadPath,
    //                //    256 * 1024,
    //                //    hashCts.Token
    //                //);

    //                //while (t.IsCompleted == false)
    //                //{
    //                //    yield return null;
    //                //}

    //                //string payloadHex = t.Result;

    //                //if (string.Equals(payloadHex, ctx.remoteMeta.sha256, StringComparison.OrdinalIgnoreCase) == false)
    //                //{
    //                //    result.result = VerifyResult.Failed;
    //                //    result.failReason = VerifyFailReason.InvalidResponse;
    //                //    yield break;
    //                //}
    //            }

    //            // 3-3) Catalog 본문 로드 (이 시점에 null이면 바로 실패)
    //            ContentCatalog catalog = LoadContentPayload<ContentCatalog>(entry.id, entry.schema);
    //            if (catalog == null)
    //            {
    //                result.result = VerifyResult.Failed;
    //                result.failReason = VerifyFailReason.InvalidResponse;
    //                yield break;
    //            }

    //            // 4) Bundle 반복: "파일 hash -> entry.sha256 검증"
    //            if (catalog.bundles == null)
    //            {
    //                continue;
    //            }

    //            for (int b = 0; b < catalog.bundles.Count; b++)
    //            {
    //                ContentBundleEntry bundle = catalog.bundles[b];
    //                if (bundle == null)
    //                {
    //                    continue;
    //                }

    //                if (string.IsNullOrEmpty(bundle.id) || string.IsNullOrEmpty(bundle.sha256))
    //                {
    //                    result.result = VerifyResult.Failed;
    //                    result.failReason = VerifyFailReason.InvalidResponse;
    //                    yield break;
    //                }

    //                string bundlePath = Path.Combine(
    //                    Application.persistentDataPath,
    //                    "Bundles",
    //                    entry.schema,
    //                    bundle.id,
    //                    bundle.sha256 + ".bundle"
    //                );


    //            }
    //        }

    //        result.result = VerifyResult.UpToDate;//if (hashCts != null)
    //                                              //{
    //                                              //    hashCts.Cancel();
    //                                              //    hashCts.Dispose();
    //                                              //    hashCts = null;
    //                                              //}

    //        //hashCts = new CancellationTokenSource();

    //        //Task<string> t = Sha256StreamTask.ComputeFileHexAsync(
    //        //    bundlePath,
    //        //    256 * 1024,
    //        //    hashCts.Token
    //        //);

    //        //while (t.IsCompleted == false)
    //        //{
    //        //    yield return null;
    //        //}

    //        //string actualHex = t.Result;

    //        //if (string.Equals(actualHex, bundle.sha256, StringComparison.OrdinalIgnoreCase) == false)
    //        //{
    //        //    result.result = VerifyResult.Failed;
    //        //    result.failReason = VerifyFailReason.InvalidResponse;
    //        //    yield break;
    //        //}
    //    }

    //    public static IEnumerator SyncContentManifest()
    //    {
    //        ContentMeta localMeta;
    //        // 1. 로컬 데이터 세팅
    //        LoadContentMeta(out localMeta, contentManifest.id, contentManifest.schema);

    //        if (manifestVerifyContext == null)
    //        {
    //            manifestVerifyContext = new ContentVerifyContext();
    //        }

    //        // 2. 서버와 비교 검증
    //        yield return VerifyContentMeta(localMeta, contentManifest.id, contentManifest.schema, manifestVerifyContext);

    //        // 3, 결과 처리
    //        switch (manifestVerifyContext.result)
    //        {
    //            case VerifyResult.None:
    //                Debug.LogWarning("[ResourceManager] Content verify result is None");
    //                break;
    //            case VerifyResult.Failed:
    //                // 검증 실패
    //                // 로컬 데이터 사용
    //                // 멀티플레이 모드 금지 ==> 재검증 요구
    //                Debug.LogWarning($"[ResourceManager] Content verify failed: {manifestVerifyContext.failReason}");
    //                //GameManager.instance.ChangeGameMode(Types.GameMode.Single);
    //                break;
    //            case VerifyResult.UpToDate:
    //                // 최신 상태
    //                break;
    //            case VerifyResult.Outdated:
    //                {
    //                    Debug.Log("[ResourceManager] Content is outdated, updating...");
    //                    // 3_1 매니페스트 본문 캐시 업데이트
    //                    yield return UpdateContentManifest(manifestVerifyContext);

    //                    if (manifestVerifyContext.dataUpdateSucceeded == false)
    //                    {
    //                        // 매니페스트 업데이트 실패시 중단
    //                        // -> 로컬 폴백
    //                        break;
    //                    }

    //                    bool isContentConsistent = true;

    //                    // 3_2 캐시를 이용하여 카탈로그 업데이트 처리
    //                    foreach (ContentCatalogEntry catalogEntry in contentManifest.contentCatalogs)
    //                    {
    //                        // 필수 항목이 아닌 경우 스킵 
    //                        // -> 런타임 시점에 필요시점에 검증 처리(SceenLoad 시점)
    //                        if (catalogEntry.requiredOnBoot == false)
    //                        {
    //                            continue;
    //                        }

    //                        if (localMeta != null)
    //                        {
    //                            localMeta.Clear();
    //                        }
    //                        LoadContentMeta(out localMeta, catalogEntry.id, catalogEntry.schema);

    //                        ContentVerifyContext ctx = new ContentVerifyContext();
    //                        yield return VerifyContentMeta(localMeta, catalogEntry.id, catalogEntry.schema, ctx);

    //                        if (ctx.result == VerifyResult.Failed)
    //                        {
    //                            isContentConsistent = false;
    //                            break;
    //                        }

    //                        if (ctx.result == VerifyResult.UpToDate)
    //                        {
    //                            // 메타가 최신이면 로컬 payload가 반드시 있어야 함
    //                            ContentCatalog localCatalog = LoadContentPayload<ContentCatalog>(catalogEntry.id, catalogEntry.schema);
    //                            if (localCatalog == null)
    //                            {
    //                                ctx.result = VerifyResult.Outdated;
    //                            }
    //                            else
    //                            {
    //                                continue;
    //                            }
    //                        }

    //                        if (ctx.result != VerifyResult.Outdated)
    //                        {
    //                            isContentConsistent = false;
    //                            break;
    //                        }

    //                        yield return UpdateContentCatalog(ctx);

    //                        if (ctx.dataUpdateSucceeded == false)
    //                        {
    //                            isContentConsistent = false;
    //                            break;
    //                        }

    //                        ContentCatalog catalog = LoadContentPayload<ContentCatalog>(catalogEntry.id, catalogEntry.schema);
    //                        if (catalog == null)
    //                        {
    //                            isContentConsistent = false;
    //                            break;
    //                        }

    //                        yield return UpdateContentBundle(ctx, catalog);

    //                        if (ctx.dataUpdateSucceeded == false)
    //                        {
    //                            isContentConsistent = false;
    //                            break;
    //                        }

    //                        SaveContentMeta(ctx);
    //                    }

    //                    // 업데이트 도중에 문제가 발생했다면 중단
    //                    if (isContentConsistent == false)
    //                    {
    //                        break;
    //                    }

    //                    //SaveContentManifest(contentManifest);
    //                    SaveContentMeta(manifestVerifyContext);

    //                    break;
    //                }
    //            default:
    //                Debug.LogWarning("[ResourceManager] Unknown content verify result");
    //                break;
    //        }

    //        sceneEntries.Clear();

    //        // 게임 내에서 사용할 수 있는 씬 도메인 엔트리 등록
    //        for (int i = 0; i < contentManifest.scenes.Count; i++)
    //        {
    //            SceneEntry scene = contentManifest.scenes[i];
    //            if (scene == null)
    //            {
    //                continue;
    //            }

    //            uint staticKey;
    //            if (DomainKeyParser.TryParseStaticKey(scene.staticKey, out staticKey) == false)
    //            {
    //                continue;
    //            }

    //            sceneEntries[staticKey] = scene;
    //        }
    //    }

    //    private static IEnumerator VerifyContentMeta(ContentMeta localMeta, string id, string schema, ContentVerifyContext ctx)
    //    {
    //        if (ctx == null)
    //        {
    //            yield break;
    //        }

    //        ctx.Bind(id, schema);

    //        string platform = ContentPath.GetPlatformFolder();

    //        string remoteMetaUri = ContentPath.BuildMetaUri(
    //            contentManifest.verifyRoot,
    //            contentManifest.metaApi,
    //            id,
    //            schema,
    //            platform
    //        );

    //        if (string.IsNullOrEmpty(remoteMetaUri))
    //        {
    //            ctx.result = VerifyResult.Failed;
    //            ctx.failReason = VerifyFailReason.InvalidPath;
    //            yield break;
    //        }

    //        UnityWebRequest webRequest = UnityWebRequest.Get(remoteMetaUri);
    //        yield return webRequest.SendWebRequest();

    //        if (webRequest.result != UnityWebRequest.Result.Success)
    //        {
    //            ctx.result = VerifyResult.Failed;

    //            if (webRequest.result == UnityWebRequest.Result.ProtocolError)
    //            {
    //                long code = webRequest.responseCode;
    //                if (code >= 400 && code < 500)
    //                {
    //                    ctx.failReason = VerifyFailReason.Http4xx;
    //                }
    //                else if (code >= 500 && code < 600)
    //                {
    //                    ctx.failReason = VerifyFailReason.Http5xx;
    //                }
    //                else
    //                {
    //                    ctx.failReason = VerifyFailReason.InvalidResponse;
    //                }
    //            }
    //            else
    //            {
    //                ctx.failReason = VerifyFailReason.NetworkError;
    //            }

    //            yield break;
    //        }

    //        string json = webRequest.downloadHandler.text;

    //        if (string.IsNullOrEmpty(json))
    //        {
    //            ctx.result = VerifyResult.Failed;
    //            ctx.failReason = VerifyFailReason.InvalidResponse;
    //            yield break;
    //        }

    //        ContentMeta remoteMeta = JsonUtility.FromJson<ContentMeta>(json);
    //        if (remoteMeta == null || string.IsNullOrEmpty(remoteMeta.dataUri))
    //        {
    //            ctx.result = VerifyResult.Failed;
    //            ctx.failReason = VerifyFailReason.ParseError;
    //            yield break;
    //        }

    //        ctx.remoteMeta = remoteMeta;

    //        if (localMeta == null || string.IsNullOrEmpty(localMeta.sha256))
    //        {
    //            ctx.result = VerifyResult.Outdated;
    //            yield break;
    //        }

    //        if (string.Equals(localMeta.sha256, remoteMeta.sha256))
    //        {
    //            ctx.result = VerifyResult.UpToDate;
    //            yield break;
    //        }

    //        ctx.result = VerifyResult.Outdated;
    //    }

    //    /// <summary>
    //    /// 매니페스트  본문 업데이트
    //    /// </summary>
    //    private static IEnumerator UpdateContentManifest(ContentVerifyContext ctx)
    //    {
    //        if (ctx == null)
    //        {
    //            yield break;
    //        }

    //        ctx.dataUpdateSucceeded = false;

    //        if (ctx.remoteMeta == null || string.IsNullOrEmpty(ctx.remoteMeta.dataUri))
    //        {
    //            yield break;
    //        }

    //        UnityWebRequest req = UnityWebRequest.Get(ctx.remoteMeta.dataUri);
    //        yield return req.SendWebRequest();

    //        if (req.result != UnityWebRequest.Result.Success)
    //        {
    //            ctx.result = VerifyResult.Failed;
    //            ctx.failReason = VerifyFailReason.NetworkError;
    //            yield break;
    //        }

    //        string json = req.downloadHandler.text;

    //        if (string.IsNullOrEmpty(json))
    //        {
    //            ctx.result = VerifyResult.Failed;
    //            ctx.failReason = VerifyFailReason.InvalidResponse;
    //            yield break;
    //        }

    //        ContentManifest remoteManifest = JsonUtility.FromJson<ContentManifest>(json);

    //        if (remoteManifest == null)
    //        {
    //            ctx.result = VerifyResult.Failed;
    //            ctx.failReason = VerifyFailReason.ParseError;
    //            yield break;
    //        }

    //        contentManifest = remoteManifest;
    //        ctx.dataUpdateSucceeded = true;
    //    }

    //    /// <summary>
    //    /// 카탈로그 본문 다운로드 + 로컬 저장
    //    /// </summary>
    //    private static IEnumerator UpdateContentCatalog(ContentVerifyContext ctx)
    //    {
    //        if (ctx == null)
    //        {
    //            yield break;
    //        }

    //        ctx.dataUpdateSucceeded = false;

    //        if (ctx.remoteMeta == null || string.IsNullOrEmpty(ctx.remoteMeta.dataUri))
    //        {
    //            yield break;
    //        }

    //        UnityWebRequest req = UnityWebRequest.Get(ctx.remoteMeta.dataUri);
    //        yield return req.SendWebRequest();

    //        if (req.result != UnityWebRequest.Result.Success)
    //        {
    //            ctx.result = VerifyResult.Failed;
    //            ctx.failReason = VerifyFailReason.NetworkError;
    //            yield break;
    //        }

    //        string json = req.downloadHandler.text;

    //        if (string.IsNullOrEmpty(json))
    //        {
    //            ctx.result = VerifyResult.Failed;
    //            ctx.failReason = VerifyFailReason.InvalidResponse;
    //            yield break;
    //        }

    //        ContentCatalog remoteCatalog = JsonUtility.FromJson<ContentCatalog>(json);
    //        if (remoteCatalog == null)
    //        {
    //            ctx.result = VerifyResult.Failed;
    //            ctx.failReason = VerifyFailReason.ParseError;
    //            yield break;
    //        }

    //        // payload 저장
    //        string directory = Path.Combine(
    //            Application.persistentDataPath,
    //            "Data",
    //            ctx.targetSchema
    //        );

    //        if (Directory.Exists(directory) == false)
    //        {
    //            Directory.CreateDirectory(directory);
    //        }

    //        string payloadPath = Path.Combine(
    //            directory,
    //            ctx.targetId + ".json"
    //        );

    //        string tempPath = payloadPath + ".tmp";

    //        try
    //        {
    //            File.WriteAllText(tempPath, json);

    //            if (File.Exists(payloadPath))
    //            {
    //                File.Delete(payloadPath);
    //            }

    //            File.Move(tempPath, payloadPath);
    //        }
    //        catch
    //        {
    //            ctx.result = VerifyResult.Failed;
    //            ctx.failReason = VerifyFailReason.InvalidResponse;

    //            try
    //            {
    //                if (File.Exists(tempPath))
    //                {
    //                    File.Delete(tempPath);
    //                }
    //            }
    //            catch
    //            {
    //            }

    //            yield break;
    //        }

    //        ctx.dataUpdateSucceeded = true;
    //    }

    //    /// <summary>
    //    /// 콘텐츠 번들 업데이트
    //    /// 애셋 번들 바이너리를 관리해야 하기 때문에 함수 분리
    //    /// </summary>
    //    private static IEnumerator UpdateContentBundle(ContentVerifyContext ctx, ContentCatalog catalog)
    //    {
    //        if (ctx == null)
    //        {
    //            yield break;
    //        }

    //        ctx.dataUpdateSucceeded = false;

    //        if (catalog == null || catalog.bundles == null)
    //        {
    //            ctx.result = VerifyResult.Failed;
    //            ctx.failReason = VerifyFailReason.InvalidResponse;
    //            yield break;
    //        }

    //        string bundleRoot = Path.Combine(
    //            Application.persistentDataPath,
    //            "Bundles",
    //            ctx.targetSchema
    //        );

    //        if (Directory.Exists(bundleRoot) == false)
    //        {
    //            Directory.CreateDirectory(bundleRoot);
    //        }

    //        for (int i = 0; i < catalog.bundles.Count; i++)
    //        {
    //            ContentBundleEntry entry = catalog.bundles[i];
    //            if (entry == null)
    //            {
    //                continue;
    //            }

    //            if (string.IsNullOrEmpty(entry.id) || string.IsNullOrEmpty(entry.dataUri) || string.IsNullOrEmpty(entry.sha256))
    //            {
    //                ctx.result = VerifyResult.Failed;
    //                ctx.failReason = VerifyFailReason.InvalidResponse;
    //                yield break;
    //            }

    //            string bundleDir = Path.Combine(bundleRoot, entry.id);
    //            if (Directory.Exists(bundleDir) == false)
    //            {
    //                Directory.CreateDirectory(bundleDir);
    //            }

    //            string finalPath = Path.Combine(bundleDir, entry.sha256 + ".bundle");
    //            string tempPath = finalPath + ".tmp";

    //            if (File.Exists(finalPath))
    //            {
    //                if (entry.sizeBytes > 0)
    //                {
    //                    long len = new FileInfo(finalPath).Length;
    //                    if (len == entry.sizeBytes)
    //                    {
    //                        continue;
    //                    }

    //                    try
    //                    {
    //                        File.Delete(finalPath);
    //                    }
    //                    catch
    //                    {
    //                        ctx.result = VerifyResult.Failed;
    //                        ctx.failReason = VerifyFailReason.InvalidResponse;
    //                        yield break;
    //                    }
    //                }
    //                else
    //                {
    //                    continue;
    //                }
    //            }

    //            if (File.Exists(tempPath))
    //            {
    //                try
    //                {
    //                    File.Delete(tempPath);
    //                }
    //                catch
    //                {
    //                    ctx.result = VerifyResult.Failed;
    //                    ctx.failReason = VerifyFailReason.InvalidResponse;
    //                    yield break;
    //                }
    //            }

    //            UnityWebRequest req = UnityWebRequest.Get(entry.dataUri);
    //            yield return req.SendWebRequest();

    //            if (req.result != UnityWebRequest.Result.Success)
    //            {

    //                ctx.result = VerifyResult.Failed;
    //                ctx.failReason = VerifyFailReason.NetworkError;
    //#if UNITY_EDITOR
    //                long code = req.responseCode;
    //                Debug.LogWarning(
    //                    $"[ContentBundle] Download failed\n" +
    //                    $"Catalog : {ctx.targetId}\n" +
    //                    $"Bundle  : {entry.id}\n" +
    //                    $"Uri     : {entry.dataUri}\n" +
    //                    $"Result  : {req.result}\n" +
    //                    $"Code    : {code}\n" +
    //                    $"Error   : {req.error}"
    //                );
    //#endif
    //                yield break;
    //            }

    //            byte[] data = req.downloadHandler.data;
    //            if (data == null || data.Length == 0)
    //            {
    //                ctx.result = VerifyResult.Failed;
    //                ctx.failReason = VerifyFailReason.InvalidResponse;
    //                yield break;
    //            }

    //            try
    //            {
    //                File.WriteAllBytes(tempPath, data);

    //                if (entry.sizeBytes > 0)
    //                {
    //                    long len = new FileInfo(tempPath).Length;
    //                    if (len != entry.sizeBytes)
    //                    {
    //#if UNITY_EDITOR || DEVELOPMENT_BUILD
    //                        Debug.LogWarning(
    //                            $"[ContentBundle] Size mismatch\n" +
    //                            $"Catalog : {ctx.targetId}\n" +
    //                            $"Bundle  : {entry.id}\n" +
    //                            $"Expect  : {entry.sizeBytes}\n" +
    //                            $"Actual  : {len}"
    //                        );
    //#endif
    //                        File.Delete(tempPath);
    //                        ctx.result = VerifyResult.Failed;
    //                        ctx.failReason = VerifyFailReason.InvalidResponse;
    //                        yield break;
    //                    }
    //                }

    //                if (File.Exists(finalPath))
    //                {
    //                    File.Delete(finalPath);
    //                }

    //                File.Move(tempPath, finalPath);
    //            }
    //            catch
    //            {
    //                ctx.result = VerifyResult.Failed;
    //                ctx.failReason = VerifyFailReason.InvalidResponse;
    //#if UNITY_EDITOR || DEVELOPMENT_BUILD
    //                Debug.LogError(
    //                    $"[ContentBundle] File operation failed\n" +
    //                    $"Catalog : {ctx.targetId}\n" +
    //                    $"Bundle  : {entry.id}\n" +
    //                    $"Path    : {finalPath}"
    //                );
    //#endif
    //                try
    //                {
    //                    if (File.Exists(tempPath))
    //                    {
    //                        File.Delete(tempPath);
    //                    }
    //                }
    //                catch
    //                {
    //                }

    //                yield break;
    //            }
    //        }

    //        ctx.dataUpdateSucceeded = true;
    //    }
}