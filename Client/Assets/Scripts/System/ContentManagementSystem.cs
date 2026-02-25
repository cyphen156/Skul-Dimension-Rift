using Assets.Scripts.Content;
using Assets.Scripts.Data;
using Assets.Scripts.Utility;
using System;
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
                rm.UpsertContentEntry(catalog);
                
                if (catalog.requiredOnBoot)
                {
                    IOResult rob = await PrepareContentAsync(catalog.header.staticKey, ct);
                    if (!rob.succeed)
                    {
                        return rob;
                    }
                }
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

    /// <summary>
    /// 특정 staticKey에 해당하는 컨텐츠를 사용할 수 있도록 준비하는 과정
    /// 엔트리 유형에 따라 준비 과정이 달라질 수 있음
    /// Ex) 
    /// 1. 본문이 존재하는 데이터 유형의 경우, Codec을 통한 본문 해석 과정이 포함
    /// 2. Bundle 유형의 경우 해당 번들에 존재 여부 확인
    /// 3. 씬 엔트리의 경우, 소유한 카탈로그 엔트리의 준비 과정을 재귀적으로 호출
    /// </summary>
    /// <param name="staticKey"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static async Task<IOResult> PrepareContentAsync(uint staticKey, CancellationToken ct = default)
    {
        ResourceManager rm = ResourceManager.instance;

        if (rm == null)
        {
            return IOResult.Fail(IOFailReason.NotInitialized);
        }

        if (!rm.TryGetContentEntry(staticKey, out ContentEntry entry) || entry == null)
        {
            return IOResult.Fail(IOFailReason.NotRegistered);
        }

        // 1. 해석이 필요하지 않은 유형의 경우
        switch (entry)
        {
            // 씬 엔트리
            case SceneEntry sceneEntry:
                return await PrepareContentAsync(sceneEntry.ownerStaticKey, ct);

            // 번들 엔트리
            case ContentBundleEntry bundleEntry:
                return rm.Exists(ContentPath.GetContentLocalPath(bundleEntry));

            // Addressables 카탈로그 엔트리
            case AddressablesCatalogEntry addressablesCatalog:
                {
                    // 경로 계산
                    string addressablesCatalogPath = ContentPath.GetContentLocalPath(addressablesCatalog);

                    // 로드
                    IOResult loadResult = await rm.LoadAddressablesCatalogAsync(staticKey, addressablesCatalogPath);
                    if (!loadResult.succeed)
                    {
                        return loadResult;
                    }

                    if (!rm.TryGetAsset_Internal<IResourceLocator>(staticKey, AccessMode.Internal, out IResourceLocator locator))
                    {
                        return IOResult.Fail(IOFailReason.NotRegistered);
                    }

                    // 이미 등록된 로케이터인지 확인
                    foreach (IResourceLocator l in Addressables.ResourceLocators)
                    {
                        if (l.LocatorId.Equals(locator.LocatorId))
                        {
                            return IOResult.Ok();
                        }
                    }

                    // 로케이터 등록
                    Addressables.AddResourceLocator(locator);

                    return IOResult.Ok();
                }
            default:
                break;
        }

        // 2. 본문 해석이 필요한 유형의 경우
        // 2_1. 레코드식 데이터 읽어오기
        var read = await rm.ReadAllTextsAsync(ContentPath.GetContentLocalPath(entry), ct);

        if (!read.result.succeed || string.IsNullOrEmpty(read.data))
        {
            return read.result;
        }

        // 2_2. 레코드식 데이터 -> 본문으로 디코딩
        ContentRecord catalogRecord;
        try
        {
            catalogRecord = JsonSerializer.Deserialize<ContentRecord>(read.data, ContentRecordCodec.Options);
        }
        catch (Exception e)
        {
            return IOResult.Fail(IOFailReason.DecodeFailed, e);
        }
        
        if (!ContentRecordCodec.TryDecode(catalogRecord, out var catalogBody))
        {
            return IOResult.Fail(IOFailReason.DecodeFailed);
        }
        
        switch (catalogBody)
        {
            case ContentCatalog catalog:
                {
                    // 콘텐츠 카탈로그의 경우, DLC 패키지와 같은 개념으로 설계되었음
                    if (rm.TryGetAsset<ContentCatalog>(catalog.staticKey, out _))
                    {
                        return IOResult.Ok();
                    }
                    
                    // 2_2_1_a. 콘텐츠 카탈로그가 소유한 번들 세트 엔트리에 대한 처리
                    foreach (ContentBundleEntry bundle in catalog.bundles)
                    {
                        rm.UpsertContentEntry(bundle);
                    }
                    
                    // 2_2_1_b. Addressables 카탈로그로 등록
                    AddressablesCatalogEntry addressablesCatalog = catalog.addressablesCatalog;
                    rm.UpsertContentEntry(addressablesCatalog);
                    uint acKey = addressablesCatalog.header.staticKey;
                    IOResult acResult = await PrepareContentAsync(acKey, ct);

                    if (!acResult.succeed)
                    {
                        return acResult;
                    }

                    // 2_2_1_c. 콘텐츠 카탈로그가 소유한 데이터 세트 엔트리에 대한 처리
                    foreach (ContentDataSetEntry dateSet in catalog.dataSets)
                    {
                        rm.UpsertContentEntry(dateSet);
                    }

                    rm.Register<ContentCatalog>(catalog.staticKey, catalog, AccessMode.Public);
                    break;
                }
            // 유니티 에셋인경우
            
            // 추후 다른 유형의 엔트리가 추가될 수 있으며, 각 유형에 따른 준비 과정이 정의될 수 있음
            default:
                {
                    // 시스템이 해석할 수 없는 유형의 경우
                    return IOResult.Fail(IOFailReason.InvalidResponse);
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
            IOResult fileInfoResult = rm.TryGetFileInfo(localPath, out FileInfo downloadedInfo);
            if (!fileInfoResult.succeed)
            {
                return IOResult.Fail(IOFailReason.InvalidPath);
            }

            if (downloadedInfo.Length != bundle.sizeBytes)
            {
                await rm.Delete(localPath);
                return IOResult.Fail(IOFailReason.InvalidResponse);
            }
        }
        return IOResult.Ok();
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
        // 여기서 실패한다는 것은 엔트리구성정보가 잘못되어 있다는 것을 의미하니 동기화 실패로 폴백
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
        // 메타는 있어도 되고 없어도 됨
        // 단, 메타가 없다면 본문이 최신이 아니라는것을 의미한다고 고정함
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

        // 검증 결과 메타가 최신인데 본문이 없다는 것정도는 체크를 해줘야함
        if (ctx.verifyResult == VerifyResult.UpToDate)
        {
            string payloadPath = ContentPath.GetContentLocalPath(entry);

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

            // 번들의 경우 특수하게 사이즈까지 체크해줌
            if (entry.header.category == ContentCategory.Bundle)
            {
                ContentBundleEntry bundleEntry = entry as ContentBundleEntry;
                if (bundleEntry == null)
                {
                    ctx.verifyResult = VerifyResult.Failed;
                    ctx.failReason = SyncFailReason.TypeMismatch;
                    return ctx;
                }

                IOResult bundleIR = rm.TryGetFileInfo(payloadPath, out FileInfo bundleInfo);

                if (!bundleIR.succeed || bundleInfo == null)
                {
                    ctx.verifyResult = VerifyResult.Outdated;
                }

                // sizeBytes는 엔트리 규약상 반드시 유효값이어야 한다는 전제
                if (bundleEntry.sizeBytes <= 0)
                {
                    ctx.verifyResult = VerifyResult.Failed;
                    ctx.failReason = SyncFailReason.InvalidResponse;
                    return ctx;
                }

                // 번들 크기가 다르면 다운로드 중단/손상 가능성이 있으니 Outdated로 강등
                if (bundleInfo.Length != bundleEntry.sizeBytes)
                {
                    ctx.verifyResult = VerifyResult.Outdated;
                    ctx.failReason = SyncFailReason.None;
                    return ctx;
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