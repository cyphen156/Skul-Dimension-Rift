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
using static ResourceManager;

/// <summary>
/// 컨텐츠 관련 정책을 관리, 제공하는 시스템
/// </summary>
public static class ContentManagementSystem
{
    /// <summary>
    /// 부팅시 게임 아이덴티티 적용: 매니페스트와 카탈로그를 로드하여 ResourceManager에 등록
    /// </summary>
    /// <param name="path"></param>
    /// <param name="caller"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
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

        // AssetMap에 Manifest 등록
        if (!rm.Register(manifest.staticKey, manifest, AccessMode.Public))
        {
            return IOResult.Fail(IOFailReason.RegistrationFailed);
        }

        if (!ResourceManager.InitializeManifestStaticKey(manifest.staticKey))
        {
            return IOResult.Fail(IOFailReason.RegistrationFailed);
        }

        // 엔트리 등록
        ContentManifestEntry manifestEntry = new ContentManifestEntry();
        manifestEntry.header = localRecord.header;
        rm.UpsertContentEntry(manifestEntry);

        if (manifest.contentCatalogs != null)
        {
            foreach (ContentCatalogEntry catalog in manifest.contentCatalogs)
            {
                if (catalog.requiredOnBoot)
                {
                    string catalogPath = Path.Combine(
                        Application.persistentDataPath,
                        catalog.header.category.ToString(),
                        catalog.header.schema,
                        catalog.header.id + ".json"
                    );

                    IOResult isExists = rm.Exists(catalogPath);
                    if (!isExists.succeed)
                    {
                        return isExists;
                    }
                }
                rm.UpsertContentEntry(catalog);
            }
        }

        if (manifest.scenes != null)
        {
            for (int i = 0; i < manifest.scenes.Count; i++)
            {
                SceneEntry scene = manifest.scenes[i];
                rm.UpsertContentEntry(scene);
            }
        }
        return IOResult.Ok();
    }

    private static async Task VerifyContentMetaAsync(ContentMeta localMeta, ContentSyncContext ctx, CancellationToken ct = default)
    {
        if (ctx == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(ctx.targetId) || string.IsNullOrEmpty(ctx.targetSchema))
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.InvalidPath;
            return;
        }

        ResourceManager rm = ResourceManager.instance;
        if (rm == null)
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.NotInitialized;
            return;
        }

        ContentManifest manifest = null;
        if (!rm.TryGetAsset<ContentManifest>(ResourceManager.ManifestStaticKey, out manifest))
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.NotInitialized;
            return;
        }

        string platform = ContentPath.GetPlatformFolder();

        string remoteMetaUri = ContentPath.BuildMetaAPIUri(
            manifest.verifyRoot,
            manifest.metaApi,
            ctx.targetId,
            ctx.targetSchema,
            platform
        );

        if (string.IsNullOrEmpty(remoteMetaUri))
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.InvalidPath;
            return;
        }

        var downloaded = await rm.DownloadBufferAsync(remoteMetaUri, ct);
        IOResult ioResult = downloaded.result;
        byte[] buffer = downloaded.data;

        if (ioResult == null || !ioResult.succeed)
        {
            ctx.verifyResult = VerifyResult.Failed;

            if (ioResult == null)
            {
                ctx.failReason = SyncFailReason.NetworkError;
                return;
            }

            switch (ioResult.failReason)
            {
                case IOFailReason.Canceled:
                        ctx.failReason = SyncFailReason.Canceled;
                        break;
                case IOFailReason.InvalidUri:
                        ctx.failReason = SyncFailReason.InvalidUri;
                        break;
                case IOFailReason.InvalidPath:
                        ctx.failReason = SyncFailReason.InvalidPath;
                        break;
                case IOFailReason.Unknown:
                    {
                        ctx.failReason = SyncFailReason.InvalidResponse;
                        break;
                    }
                case IOFailReason.DecodeFailed:
                        ctx.failReason = SyncFailReason.ParseError;
                        break;
                default:
                        ctx.failReason = SyncFailReason.NetworkError;
                        break;
            }

            return;
        }

        if (buffer == null || buffer.Length == 0)
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.InvalidResponse;
            return;
        }

        string json = Encoding.UTF8.GetString(buffer);
        if (string.IsNullOrEmpty(json))
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.InvalidResponse;
            return;
        }

        ContentMeta remoteMeta = null;

        try
        {
            remoteMeta =
                JsonSerializer.Deserialize<ContentMeta>(json, ContentRecordCodec.Options);
        }
        catch
        {
            remoteMeta = null;
        }

        if (remoteMeta == null || string.IsNullOrEmpty(remoteMeta.sha256))
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.ParseError;
            return;
        }

        ctx.remoteMeta = remoteMeta;

        if (localMeta == null || string.IsNullOrEmpty(localMeta.sha256))
        {
            ctx.verifyResult = VerifyResult.Outdated;
            return;
        }

        if (string.Equals(localMeta.sha256, remoteMeta.sha256, StringComparison.OrdinalIgnoreCase))
        {
            ctx.verifyResult = VerifyResult.UpToDate;
            return;
        }

        ctx.verifyResult = VerifyResult.Outdated;
        return;
    }

    private static async Task<IOResult> UpdateContentAsync(ContentSyncContext ctx, CancellationToken ct = default)
    {
        if (ctx == null)
        {
            return IOResult.Fail(IOFailReason.InvalidResponse);
        }

        if (ctx.verifyResult != VerifyResult.Outdated)
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
                        ctx.category.ToString(),
                        ctx.targetSchema,
                        ctx.targetId
            );

        switch (ctx.category)
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

    private static async Task<IOResult> UpdateBundleAsync(ContentBundleEntry bundle, CancellationToken ct = default)
    {
        if (bundle == null)
        {
            return IOResult.Fail(IOFailReason.InvalidResponse);
        }

        if (string.IsNullOrEmpty(bundle.sha256) || string.IsNullOrEmpty(bundle.dataUri))
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
            bundle.header.category.ToString(),
            bundle.header.schema,
            bundle.header.id,
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
    
    /// <summary>
    /// staticKey에 해당하는 컨텐츠를 서버를 기준으로 최신화 시도
    /// </summary>
    /// <param name="staticKey"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<ContentSyncContext> SyncContentAsync(uint staticKey, CancellationToken ct = default)
    {
        // 1. 로컬 데이터 세팅
        ResourceManager rm = ResourceManager.instance;

        ContentSyncContext ctx = new ContentSyncContext();
        ctx.Bind(staticKey, string.Empty, string.Empty, ContentCategory.None);

        if (rm == null)
        {
            ctx.syncResult = SyncResult.Failed;
            ctx.failReason = SyncFailReason.NotInitialized;
            return ctx;
        }

        // 헤더에는 최소 식별정보가 들어있음 -> 엔트리 맵이 필요함
        if (!rm.TryGetContentEntry(staticKey, out ContentEntry entry))
        {
            ctx.syncResult = SyncResult.Failed;
            ctx.failReason = SyncFailReason.NotFound;
            return ctx;
        }

        ContentHeader header = entry.header;
        ctx.Bind(staticKey, header.id, header.schema, header.category);

        if (string.IsNullOrEmpty(header.id) || string.IsNullOrEmpty(header.schema))
        {
            ctx.syncResult = SyncResult.Failed;
            ctx.failReason = SyncFailReason.InvalidPath;
            return ctx;
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
            ctx.syncResult = SyncResult.Failed;
            ctx.failReason = SyncFailReason.InvalidResponse;
            return ctx;
        }

        await VerifyContentMetaAsync(localMeta, ctx, ct);

        if (ctx.verifyResult == VerifyResult.UpToDate)
        {
            string payloadPath = Path.Combine(
                Application.persistentDataPath,
                header.category.ToString(),
                header.schema,
                header.id + ".json");

            IOResult payloadIR = rm.Exists(payloadPath);

            if (!payloadIR.succeed)
            {
                ctx.verifyResult = VerifyResult.Outdated;

                switch (payloadIR.failReason)
                {
                    case IOFailReason.NotFound:
                        ctx.failReason = SyncFailReason.NotFound;
                        break;
                    case IOFailReason.InvalidPath:
                        ctx.failReason = SyncFailReason.InvalidPath;
                        break;
                    case IOFailReason.AccessDenied:
                        ctx.failReason = SyncFailReason.AccessDenied;
                        break;
                    default:
                        ctx.failReason = SyncFailReason.InvalidResponse;
                        break;
                }
            }
        }

        switch (ctx.verifyResult)
        {
            case VerifyResult.UpToDate:
                {
                    ctx.syncResult = SyncResult.UpToDate;
                    break;
                }
            case VerifyResult.Outdated:
                {
                    // 본문 업데이트
                    IOResult ir = await UpdateContentAsync(ctx, ct);

                    if (!ir.succeed)
                    {
                        ctx.syncResult = SyncResult.Failed;
                        break;
                    }

                    ContentRecord newMetaRecord = ContentRecordCodec.Encode<ContentMeta>(
                        staticKey,
                        header.id,
                        ctx.remoteMeta.version,
                        header.schema,
                        ContentCategory.Meta,
                        ctx.remoteMeta
                    );

                    string newMetaJson = JsonSerializer.Serialize(newMetaRecord, ContentRecordCodec.Options);
                    await rm.SaveTextAsync(localMetaPath, newMetaJson, ct);
                    ctx.syncResult = SyncResult.Updated;
                    break;
                }
            case VerifyResult.Failed:
                ctx.syncResult = SyncResult.Failed;
                break;
            default:
                ctx.syncResult = SyncResult.Failed;
                break;
        }
        return ctx;
    }

    /// <summary>
    /// 컨텐츠의 일관성을 검증
    /// 주 사용처 : 멀티플레이 진입시 호출
    /// Consistency Check는 로컬 파일의 정합성 검증에 초점이 맞춰져 있음
    /// 서버에서 내려준 메타에 기재된 해시값과 로컬 파일의 본문 해시 계산값이 일치하는지를 검증
    /// 즉, 
    /// Meta, Payload 모두일치해야만 하는 검증하는 과정
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<ContentSyncContext> CheckContentConsistencyAsync(uint staticKey, CancellationToken ct = default)
    {
        // 1. 로컬 데이터 세팅
        ResourceManager rm = ResourceManager.instance;

        ContentSyncContext ctx = new ContentSyncContext();
        ctx.Bind(staticKey, string.Empty, string.Empty, ContentCategory.None);

        if (rm == null)
        {
            ctx.syncResult = SyncResult.Failed;
            ctx.failReason = SyncFailReason.NotInitialized;
            return ctx;
        }

        // 헤더에는 최소 식별정보가 들어있음 -> 엔트리 맵이 필요함
        if (!rm.TryGetContentEntry(staticKey, out ContentEntry entry))
        {
            ctx.syncResult = SyncResult.Failed;
            ctx.failReason = SyncFailReason.NotFound;
            return ctx;
        }

        ContentHeader header = entry.header;
        ctx.Bind(staticKey, header.id, header.schema, header.category);

        if (string.IsNullOrEmpty(header.id) || string.IsNullOrEmpty(header.schema))
        {
            ctx.syncResult = SyncResult.Failed;
            ctx.failReason = SyncFailReason.InvalidPath;
            return ctx;
        }

        // 헤더에서 추출한 데이터를 통해 애셋의 로컬경로에 메타가 있는지를 확인함
        string localMetaPath = Path.Combine(
            Application.persistentDataPath,
            ContentCategory.Meta.ToString(),
            header.schema,
            header.id + ".meta.json"
        );

        // 정합성 검증에서는 LocalMeta가 없거나 해시값이 비어있는 경우는 검증 실패로 간주함
        ContentMeta localMeta = null;

        // 로컬 메타 읽어오기 시도
        var (readResult, metaJson) = await rm.ReadAllTextsAsync(localMetaPath, ct);

        if (!readResult.succeed || string.IsNullOrEmpty(metaJson))
        {
            switch (readResult.failReason)
            {
                case IOFailReason.NotFound:
                    ctx.failReason = SyncFailReason.NotFound;
                    break;
                case IOFailReason.InvalidPath:
                    ctx.failReason = SyncFailReason.InvalidPath;
                    break;
                case IOFailReason.AccessDenied:
                    ctx.failReason = SyncFailReason.AccessDenied;
                    break;
                default:
                    ctx.failReason = SyncFailReason.InvalidResponse;
                    break;
            }

            ctx.syncResult = SyncResult.Failed;
            return ctx;
        }

        // 메타가 존재하지만 파싱에 실패하는 경우도 검증 실패로 간주함
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
            ctx.failReason = SyncFailReason.ParseError;
            return ctx;
        }

        await VerifyContentMetaAsync(localMeta, ctx, ct);

        if (ctx.verifyResult != VerifyResult.UpToDate)
        {
            ctx.syncResult = SyncResult.Failed;
            return ctx;
        }

        // 메타가 유효하다고 판단된 경우, 메타에 기재된 해시값과 로컬 파일의 본문 해시 계산값이 일치하는지를 검증함
        string payloadPath = Path.Combine(
            Application.persistentDataPath,
            header.category.ToString(),
            header.schema,
            header.id + ".json");

        IOResult payloadIR = rm.Exists(payloadPath);
        if (!payloadIR.succeed)
        {
            switch (payloadIR.failReason)
            {
                case IOFailReason.NotFound:
                    ctx.failReason = SyncFailReason.NotFound;
                    break;
                case IOFailReason.InvalidPath:
                    ctx.failReason = SyncFailReason.InvalidPath;
                    break;
                case IOFailReason.AccessDenied:
                    ctx.failReason = SyncFailReason.AccessDenied;
                    break;
                default:
                    ctx.failReason = SyncFailReason.InvalidResponse;
                    break;
            }
            ctx.syncResult = SyncResult.Failed;
            return ctx;
        }

        string ownerTag = $"ConsistencyCheck-{Guid.NewGuid()}";

        if (!rm.TryGetStreamContainer(payloadPath, ownerTag, out StreamContainer payloadStream, out string reason))
        {
            Debug.LogError($"Failed to get stream for payload at {payloadPath}. reason={reason}");
            ctx.failReason = SyncFailReason.InvalidResponse;
            ctx.syncResult = SyncResult.Failed;
            return ctx;
        }

        Task<string> hashTask = null;

        try
        {
            if (payloadStream == null || !payloadStream.Succeeded || payloadStream.Stream == null)
            {
                ctx.failReason = SyncFailReason.InvalidResponse;
                ctx.syncResult = SyncResult.Failed;
                return ctx;
            }

            hashTask = HashUtility.ComputeHexAsync(payloadStream.Stream, 256 * 1024, ct);

            // 작업 완료 시점에 RM이 스트림을 강제 회수하도록 바인딩
            rm.BindTask(payloadStream, hashTask);

            string computedHash = await hashTask;

            if (string.IsNullOrEmpty(computedHash))
            {
                if (ct.IsCancellationRequested)
                {
                    ctx.failReason = SyncFailReason.Canceled;
                }
                else
                {
                    ctx.failReason = SyncFailReason.InvalidResponse;
                }

                ctx.syncResult = SyncResult.Failed;
                return ctx;
            }

            if (localMeta == null || string.IsNullOrEmpty(localMeta.sha256))
            {
                ctx.failReason = SyncFailReason.InvalidResponse;
                ctx.syncResult = SyncResult.Failed;
                return ctx;
            }

            if (!string.Equals(computedHash, localMeta.sha256, StringComparison.OrdinalIgnoreCase))
            {
                // “메타는 UpToDate인데 payload가 다름”이면 정합성 깨짐
                ctx.verifyResult = VerifyResult.Outdated;
                ctx.syncResult = SyncResult.Failed;
                return ctx;
            }

            ctx.failReason = SyncFailReason.None;
            ctx.syncResult = SyncResult.UpToDate;
            return ctx;
        }
        catch (OperationCanceledException)
        {
            ctx.failReason = SyncFailReason.Canceled;
            ctx.syncResult = SyncResult.Failed;
            return ctx;
        }
        catch (Exception e)
        {
            Debug.LogError($"Consistency check failed. path={payloadPath}, ex={e}");
            ctx.failReason = SyncFailReason.InvalidResponse;
            ctx.syncResult = SyncResult.Failed;
            return ctx;
        }
        finally
        {
            // 만약 hashTask가 아직 완료되지 않은 상태에서 예외가 발생하더라도, finally 블록에서 스트림이 회수되도록 보장
            if (payloadStream != null)
            {
                payloadStream.Release();
            }
        }
    }
}