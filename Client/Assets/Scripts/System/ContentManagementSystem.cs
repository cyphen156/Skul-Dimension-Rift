using Assets.Scripts.Content;
using Assets.Scripts.Data;
using Assets.Scripts.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using static Assets.Scripts.Content.ContentPolicy;
using static ResourceManager;

/// <summary>
/// 컨텐츠 관련 정책을 관리, 제공하는 시스템
/// </summary>
public static class ContentManagementSystem
{
    public static async Task<IOResult> ApplyGameIdentityAsync(string path, Type caller, CancellationToken ct = default)
    {
        // 1. 허용되지 않은 호출자면 거부
        if (caller == null || caller != typeof(BootStrap))
        {
            UnityEngine.Debug.LogError(
                $"[Access Denied] {(caller == null ? "null" : caller.Name)}은 이 함수를 호출할 권한이 없습니다."
            );
            return IOResult.Fail(IOFailReason.AccessDenied);
        }

        if (string.IsNullOrEmpty(path))
        {
            return IOResult.Fail(IOFailReason.InvalidPath);
        }

        ResourceManager rm = ResourceManager.instance;
        if (rm == null)
        {
            return IOResult.Fail(IOFailReason.LoadFailed);
        }

        // defaultContentManifest적재
        (IOResult result, TextAsset asset) loaded =
                    await rm.LoadDefaultAssetAsync<TextAsset>(path, ct);

        if (loaded.result == null || loaded.result.succeed == false)
        {
            return loaded.result ?? IOResult.Fail(IOFailReason.LoadFailed);
        }

        if (loaded.asset == null || string.IsNullOrEmpty(loaded.asset.text))
        {
            return IOResult.Fail(IOFailReason.LoadFailed);
        }

        ContentRecord defaultManifestRecord;

        try
        {
            defaultManifestRecord =
                JsonSerializer.Deserialize<ContentRecord>(loaded.asset.text, ContentJsonOptions.Options);
        }
        catch (Exception e)
        {
            return IOResult.Fail(IOFailReason.LoadFailed, e);
        }

        if (defaultManifestRecord == null)
        {
            return IOResult.Fail(IOFailReason.LoadFailed);
        }

        if (!ContentRecordCodec.TryDecode(defaultManifestRecord, out var body))
        {
            return IOResult.Fail(IOFailReason.LoadFailed);
        }

        if (body is not ContentManifest manifest)
        {
            return IOResult.Fail(IOFailReason.DecodeFailed);
        }

        string localManifestPath = Path.Combine(
            Application.persistentDataPath,
            defaultManifestRecord.header.category.ToString(),
            defaultManifestRecord.header.schema,
            defaultManifestRecord.header.id + ".json"
        );

        // 이 과정은 실패할 수도 있지만 허용함 다음 게임 실행 또는 파일 최신화 검증과정에서 다시하면 됨
        IOResult existsResult = rm.Exists(localManifestPath);

        if (!existsResult.succeed)
        {
            if (existsResult.failReason != IOFailReason.NotFound)
            {
                return existsResult;
            }

            byte[] jsonBytes = Encoding.UTF8.GetBytes(loaded.asset.text);
            IOResult saveResult = await rm.SaveAsync(localManifestPath, jsonBytes, ct);

            if (!saveResult.succeed)
            {
                return saveResult;
            }
        }

        // 로컬 Manifest기준으로 다시 읽어옴 (PersistancePath)
        (IOResult readResult, string localJson) read = await rm.ReadAllTextsAsync(localManifestPath, ct);
        if (!read.readResult.succeed)
        {
            return read.readResult;
        }

        ContentRecord localRecord;
        try
        {
            localRecord = JsonSerializer.Deserialize<ContentRecord>(read.localJson, ContentJsonOptions.Options);
        }
        catch (Exception e)
        {
            return IOResult.Fail(IOFailReason.LoadFailed, e);
        }

        if (localRecord == null)
        {
            return IOResult.Fail(IOFailReason.LoadFailed);
        }

        if (!ContentRecordCodec.TryDecode(localRecord, out var localBody))
        {
            return IOResult.Fail(IOFailReason.DecodeFailed);
        }

        if (localBody is not ContentManifest localManifest)
        {
            return IOResult.Fail(IOFailReason.DecodeFailed);
        }

        manifest = localManifest;

        if (!rm.Register(manifest.staticKey, manifest, AccessMode.Public))
        {
            return IOResult.Fail(IOFailReason.RegistrationFailed);
        }

        if(!ResourceManager.InitializeManifestStaticKey(manifest.staticKey))
        {
            return IOResult.Fail(IOFailReason.RegistrationFailed);
        }

        return IOResult.Ok();
    }

    private static async Task<ContentVerifyContext> VerifyContentMetaAsync(ContentMeta localMeta, string id, string schema, ContentCategory category, CancellationToken ct = default)
    {
        ContentVerifyContext ctx = new ContentVerifyContext();
        ctx.Bind(id, schema, category);

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(schema))
        {
            ctx.result = VerifyResult.Failed;
            ctx.failReason = VerifyFailReason.InvalidPath;
            return ctx;
        }

        ResourceManager rm = ResourceManager.instance;
        if (rm == null)
        {
            ctx.result = VerifyResult.Failed;
            ctx.failReason = VerifyFailReason.NotInitialized;
            return ctx;
        }

        ContentManifest manifest = null;
        if (!rm.TryGetAsset<ContentManifest>(ResourceManager.ManifestStaticKey, out manifest))
        {
            ctx.result = VerifyResult.Failed;
            ctx.failReason = VerifyFailReason.NotInitialized;
            return ctx;
        }

        string platform = ContentPath.GetPlatformFolder();

        string remoteMetaUri = ContentPath.BuildMetaAPIUri(
            manifest.verifyRoot,
            manifest.metaApi,
            id,
            schema,
            platform
        );

        if (string.IsNullOrEmpty(remoteMetaUri))
        {
            ctx.result = VerifyResult.Failed;
            ctx.failReason = VerifyFailReason.InvalidPath;
            return ctx;
        }

        var downloaded = await rm.DownloadBufferAsync(remoteMetaUri, ct);
        IOResult ioResult = downloaded.result;
        byte[] buffer = downloaded.data;

        if (ioResult == null || !ioResult.succeed)
        {
            ctx.result = VerifyResult.Failed;

            if (ioResult == null)
            {
                ctx.failReason = VerifyFailReason.NetworkError;
                return ctx;
            }

            switch (ioResult.failReason)
            {
                case IOFailReason.Canceled:
                        ctx.failReason = VerifyFailReason.Canceled;
                        break;
                case IOFailReason.InvalidUri:
                        ctx.failReason = VerifyFailReason.InvalidUri;
                        break;
                case IOFailReason.InvalidPath:
                        ctx.failReason = VerifyFailReason.InvalidPath;
                        break;
                case IOFailReason.Unknown:
                    {
                        ctx.failReason = VerifyFailReason.InvalidResponse;
                        break;
                    }
                case IOFailReason.DecodeFailed:
                        ctx.failReason = VerifyFailReason.ParseError;
                        break;
                default:
                        ctx.failReason = VerifyFailReason.NetworkError;
                        break;
            }

            return ctx;
        }

        if (buffer == null || buffer.Length == 0)
        {
            ctx.result = VerifyResult.Failed;
            ctx.failReason = VerifyFailReason.InvalidResponse;
            return ctx;
        }

        string json = Encoding.UTF8.GetString(buffer);
        if (string.IsNullOrEmpty(json))
        {
            ctx.result = VerifyResult.Failed;
            ctx.failReason = VerifyFailReason.InvalidResponse;
            return ctx;
        }

        ContentMeta remoteMeta = null;

        try
        {
            remoteMeta = JsonUtility.FromJson<ContentMeta>(json);
        }
        catch (Exception)
        {
            ctx.result = VerifyResult.Failed;
            ctx.failReason = VerifyFailReason.ParseError;
            return ctx;
        }

        if (remoteMeta == null || string.IsNullOrEmpty(remoteMeta.sha256))
        {
            ctx.result = VerifyResult.Failed;
            ctx.failReason = VerifyFailReason.ParseError;
            return ctx;
        }

        ctx.remoteMeta = remoteMeta;

        if (localMeta == null || string.IsNullOrEmpty(localMeta.sha256))
        {
            ctx.result = VerifyResult.Outdated;
            return ctx;
        }

        if (string.Equals(localMeta.sha256, remoteMeta.sha256, StringComparison.Ordinal))
        {
            ctx.result = VerifyResult.UpToDate;
            return ctx;
        }

        ctx.result = VerifyResult.Outdated;
        return ctx;
    }

    private static async Task<IOResult> UpdateContentAsync(ContentVerifyContext ctx, ContentCategory category, CancellationToken ct = default)
    {
        if (ctx == null)
        {
            return IOResult.Fail(IOFailReason.InvalidResponse);
        }

        if (ctx.result != VerifyResult.Outdated)
        {
            return IOResult.Ok();
        }

        if (ctx.remoteMeta == null || string.IsNullOrEmpty(ctx.remoteMeta.dataUri))
        {
            return IOResult.Fail(IOFailReason.InvalidResponse);
        }

        if (string.IsNullOrEmpty(ctx.targetId) || string.IsNullOrEmpty(ctx.targetSchema))
        {
            return IOResult.Fail(IOFailReason.InvalidPath);
        }

        ResourceManager rm = ResourceManager.instance;
        if (rm == null)
        {
            return IOResult.Fail(IOFailReason.LoadFailed);
        }

        string localPath = Path.Combine(
            Application.persistentDataPath,
                        category.ToString(),
                        ctx.targetSchema,
                        ctx.targetId
            );

        switch (category)
        {
            case ContentCategory.Data:
                {
                    localPath += ".json";
                    break;
                }

            case ContentCategory.Meta:
                {
                    localPath += ".meta.json";
                    break;
                }
            default:
                return IOResult.Fail(IOFailReason.InvalidResponse);
        }

        IOResult r = await rm.DownloadFileAsync(ctx.remoteMeta.dataUri, localPath, ct);
        if (r == null || !r.succeed)
        {
            return r ?? IOResult.Fail(IOFailReason.NetworkError);
        }

        ctx.dataUpdateSucceeded = true;
        return r;
    }

    private static async Task<IOResult> UpdateBundleAsync(string schema, ContentBundleEntry bundle, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(schema))
        {
            return IOResult.Fail(IOFailReason.InvalidPath);
        }

        if (bundle == null)
        {
            return IOResult.Fail(IOFailReason.InvalidResponse);
        }

        if (string.IsNullOrEmpty(bundle.id) || string.IsNullOrEmpty(bundle.sha256) || string.IsNullOrEmpty(bundle.dataUri))
        {
            return IOResult.Fail(IOFailReason.InvalidResponse);
        }

        ResourceManager rm = ResourceManager.instance;
        if (rm == null)
        {
            return IOResult.Fail(IOFailReason.LoadFailed);
        }

        string localPath = Path.Combine(
            Application.persistentDataPath,
            "Bundle",
            schema,
            bundle.id,
            bundle.sha256 + ".bundle"
        );

        IOResult ir = rm.TryGetFileInfo(localPath, out FileInfo info);

        if (ir.succeed)
        {
            if (bundle.sizeBytes > 0 && info.Length == bundle.sizeBytes)
            {
                return IOResult.Ok();
            }
        }
        else
        {
            if (ir.failReason != IOFailReason.NotFound)
            {
                return ir;
            }
        }

        IOResult r = await rm.DownloadFileAsync(bundle.dataUri, localPath, ct);
        
        if (r == null || !r.succeed)
        {
            return r ?? IOResult.Fail(IOFailReason.NetworkError);
        }

        if (bundle.sizeBytes > 0)
        {
            long len;

            try
            {
                len = new FileInfo(localPath).Length;
            }
            catch (Exception e)
            {
                return IOResult.Fail(IOFailReason.InvalidPath, e);
            }

            if (len != bundle.sizeBytes)
            {
                // 삭제 시도는 실패할 수 잇음
                await rm.Delete(localPath);

                return IOResult.Fail(IOFailReason.InvalidResponse);
            }
        }
        return IOResult.Ok();
    }

    private static void ApplyAddressablesLocator(List<IResourceLocator> registers, List<IResourceLocator> unregisters)
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

    /// <summary>
    /// staticKey에 해당하는 컨텐츠를 서버를 기준으로 최신화 시도
    /// </summary>
    /// <param name="staticKey"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<SyncResult> SyncContentAsync(uint staticKey, CancellationToken ct = default)
    {
        // 1. 로컬 데이터 세팅
        ResourceManager rm = ResourceManager.instance;

        if (rm == null)
        {
            return SyncResult.Failed;
        }

        // 헤더에는 최소 식별정보가 들어있음 -> 엔트리 맵이 필요함
        if (!rm.TryGetContentEntry(staticKey, out ContentEntry entry))
        {
            return SyncResult.Failed;
        }

        ContentHeader header = entry.header;

        if (string.IsNullOrEmpty(header.id) || string.IsNullOrEmpty(header.schema))
        {
            return SyncResult.Failed;
        }

        // 헤더에서 추출한 데이터를 통해 애셋의 로컬경로에 메타가 있는지를 확인함
        string localMetaPath = Path.Combine(
            Application.persistentDataPath,
            ContentCategory.Meta.ToString(),
            header.schema,
            header.id + ".meta.json"
        );

        ContentMeta localMeta = null;

        IOResult existsResult = rm.Exists(localMetaPath);

        if (existsResult.succeed)
        {
            var (readResult, metaJson) = await rm.ReadAllTextsAsync(localMetaPath, ct);

            if (readResult != null && readResult.succeed && !string.IsNullOrEmpty(metaJson))
            {
                try
                {
                    ContentRecord localMetaRecord =
                        JsonSerializer.Deserialize<ContentRecord>(metaJson, ContentRecordCodec.Options);

                    if (localMetaRecord != null)
                    {
                        ContentRecordCodec.TryDecode<ContentMeta>(localMetaRecord, out localMeta);
                    }
                }
                catch
                {
                    localMeta = null;
                }
            }
        }

        if (!existsResult.succeed && existsResult.failReason != IOFailReason.NotFound)
        {
            return SyncResult.Failed;
        }

        ContentVerifyContext verifyCtx = await VerifyContentMetaAsync(localMeta, header.id, header.schema, header.category, ct);

        if (verifyCtx == null)
        {
            return SyncResult.Failed;
        }

        if (verifyCtx.result == VerifyResult.UpToDate)
        {
            string payloadPath = Path.Combine(
                Application.persistentDataPath,
                header.category.ToString(),
                header.schema,
                header.id + ".json");

            IOResult payloadIR = rm.Exists(payloadPath);

            if (!payloadIR.succeed)
            {
                verifyCtx.result = VerifyResult.Outdated;

                switch (payloadIR.failReason)
                {
                    case IOFailReason.NotFound:
                        verifyCtx.failReason = VerifyFailReason.NotFound;
                        break;
                    case IOFailReason.InvalidPath:
                        verifyCtx.failReason = VerifyFailReason.InvalidPath;
                        break;
                    case IOFailReason.AccessDenied:
                        verifyCtx.failReason = VerifyFailReason.AccessDenied;
                        break;
                    default:
                        verifyCtx.failReason = VerifyFailReason.InvalidResponse;
                        break;
                }
            }
        }

        SyncResult result;

        switch (verifyCtx.result)
        {
            case VerifyResult.UpToDate:
                {
                    result = SyncResult.UpToDate;
                    break;
                }
            case VerifyResult.Outdated:
                {
                    // 본문 업데이트
                    IOResult ir = await UpdateContentAsync(verifyCtx, header.category, ct);

                    if (!ir.succeed)
                    {
                        result = SyncResult.Failed;
                        break;
                    }

                    ContentRecord newMetaRecord = ContentRecordCodec.Encode<ContentMeta>(
                        staticKey,
                        header.id,
                        verifyCtx.remoteMeta.version,
                        header.schema,
                        ContentCategory.Meta,
                        verifyCtx.remoteMeta
                    );

                    string newMetaJson = JsonSerializer.Serialize(newMetaRecord, ContentRecordCodec.Options);
                    await rm.SaveTextAsync(localMetaPath, newMetaJson, ct);
                    result = SyncResult.Updated;
                    break;
                }
            case VerifyResult.Failed:
                result = SyncResult.Failed;
                break;
            default:
                result = SyncResult.Failed;
                break;
        }
        return result;
    }

//    return VerifyContentMeta(localMeta, contentManifest.id, contentManifest.schema, manifestVerifyContext);

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
//                    return UpdateContentManifest(manifestVerifyContext);

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
//LoadContentMeta(out localMeta, catalogEntry.id, catalogEntry.schema);

//ContentVerifyContext ctx = new ContentVerifyContext();
//return VerifyContentMeta(localMeta, catalogEntry.id, catalogEntry.schema, ctx);

//if (ctx.result == VerifyResult.Failed)
//{
//    isContentConsistent = false;
//    break;
//}

//if (ctx.result == VerifyResult.UpToDate)
//{
//    // 메타가 최신이면 로컬 payload가 반드시 있어야 함
//    ContentCatalog localCatalog = LoadContentPayload<ContentCatalog>(catalogEntry.id, catalogEntry.schema);
//    if (localCatalog == null)
//    {
//        ctx.result = VerifyResult.Outdated;
//    }
//    else
//    {
//        continue;
//    }
//}

//if (ctx.result != VerifyResult.Outdated)
//{
//    isContentConsistent = false;
//    break;
//}

//return UpdateContentCatalog(ctx);

//if (ctx.dataUpdateSucceeded == false)
//{
//    isContentConsistent = false;
//    break;
//}

//ContentCatalog catalog = LoadContentPayload<ContentCatalog>(catalogEntry.id, catalogEntry.schema);
//if (catalog == null)
//{
//    isContentConsistent = false;
//    break;
//}

//return UpdateContentBundle(ctx, catalog);

//if (ctx.dataUpdateSucceeded == false)
//{
//    isContentConsistent = false;
//    break;
//}

//SaveContentMeta(ctx);
//                    }

//                    // 업데이트 도중에 문제가 발생했다면 중단
//                    if (isContentConsistent == false)
//{
//    break;
//}

////SaveContentManifest(contentManifest);
//SaveContentMeta(manifestVerifyContext);

//break;
//                }
//            default:
//                Debug.LogWarning("[ResourceManager] Unknown content verify result");
//break;
//        }

//        sceneEntries.Clear();

//// 게임 내에서 사용할 수 있는 씬 도메인 엔트리 등록
//for (int i = 0; i < contentManifest.scenes.Count; i++)
//{
//    SceneEntry scene = contentManifest.scenes[i];
//    if (scene == null)
//    {
//        continue;
//    }

//    uint staticKey;
//    if (DomainKeyParser.TryParseStaticKey(scene.staticKey, out staticKey) == false)
//    {
//        continue;
//    }

//    sceneEntries[staticKey] = scene;
//}
}