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
            string callerName = caller == null ? "null" : caller.Name;

            throw new UnauthorizedAccessException(
                $"[GameIdentity.DefaultManifest.AccessDenied] 호출 권한이 없습니다. caller={callerName}"
            );
        }

        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException(
                "[GameIdentity.DefaultManifest.InvalidPath] 기본 Manifest Resources 경로가 비어 있습니다.",
                nameof(path)
            );
        }

        ResourceManager rm = ResourceManager.instance;
        if (rm == null)
        {
            throw new InvalidOperationException(
                "[GameIdentity.DefaultManifest.NotInitialized] ResourceManager.instance가 초기화되지 않았습니다."
            );
        }

        // defaultContentManifest 적재
        (IOResult result, TextAsset asset) loaded =
            await rm.LoadDefaultAssetAsync<TextAsset>(path, ct);

        if (loaded.result == null)
        {
            throw new InvalidOperationException(
                $"[GameIdentity.DefaultManifest.NullResult] 기본 Manifest 로드 결과가 null입니다. path={path}"
            );
        }

        if (loaded.result.failReason == IOFailReason.Canceled)
        {
            throw new OperationCanceledException(
                $"[GameIdentity.DefaultManifest.Canceled] 기본 Manifest 로드가 취소되었습니다. path={path}",
                loaded.result.exception,
                ct
            );
        }

        if (loaded.result.succeed == false)
        {
            throw new IOException(
                $"[GameIdentity.DefaultManifest.LoadFailed] 기본 Manifest 로드에 실패했습니다. path={path}, reason={loaded.result.failReason}",
                loaded.result.exception
            );
        }

        if (loaded.asset == null)
        {
            throw new InvalidDataException(
                $"[GameIdentity.DefaultManifest.NullAsset] 기본 Manifest TextAsset이 null입니다. path={path}"
            );
        }

        if (string.IsNullOrEmpty(loaded.asset.text))
        {
            throw new InvalidDataException(
                $"[GameIdentity.DefaultManifest.EmptyText] 기본 Manifest 내용이 비어 있습니다. path={path}"
            );
        }

        ContentRecord defaultManifestRecord;

        try
        {
            defaultManifestRecord =
                JsonSerializer.Deserialize<ContentRecord>(loaded.asset.text, ContentJsonOptions.Options);
        }
        catch (JsonException e)
        {
            throw new JsonException(
                $"[GameIdentity.DefaultManifest.JsonParseFailed] 기본 Manifest JSON 파싱에 실패했습니다. path={path}",
                e
            );
        }
        catch (Exception e)
        {
            throw new InvalidDataException(
                $"[GameIdentity.DefaultManifest.DeserializeFailed] 기본 Manifest 역직렬화 중 예외가 발생했습니다. path={path}",
                e
            );
        }

        if (defaultManifestRecord == null)
        {
            throw new InvalidDataException(
                $"[GameIdentity.DefaultManifest.NullRecord] 기본 Manifest ContentRecord가 null입니다. path={path}"
            );
        }

        if (!ContentRecordCodec.TryDecode(defaultManifestRecord, out var body))
        {
            throw new InvalidDataException(
                $"[GameIdentity.DefaultManifest.DecodeFailed] 기본 Manifest ContentRecord 디코딩에 실패했습니다. path={path}, id={defaultManifestRecord.header.id}, schema={defaultManifestRecord.header.schema}"
            );
        }

        if (body is not ContentManifest manifest)
        {
            string bodyType = body == null ? "null" : body.GetType().Name;

            throw new InvalidDataException(
                $"[GameIdentity.DefaultManifest.InvalidBodyType] 기본 Manifest 본문 타입이 ContentManifest가 아닙니다. path={path}, bodyType={bodyType}"
            );
        }

        string localManifestPath = Path.Combine(
            Application.persistentDataPath,
            defaultManifestRecord.header.category.ToString(),
            defaultManifestRecord.header.schema,
            defaultManifestRecord.header.id + ".json"
        ); 
        
        ContentManifestEntry manifestEntry = new ContentManifestEntry();
        manifestEntry.header = defaultManifestRecord.header;

        // 이 과정은 실패할 수도 있지만 허용함 다음 게임 실행 또는 파일 최신화 검증과정에서 다시하면 됨
        IOResult existsResult = rm.Exists(localManifestPath);

        if (!existsResult.succeed)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(loaded.asset.text);
            await rm.SaveAsync(localManifestPath, jsonBytes, ct);
        }

        // 로컬 Manifest기준으로 다시 읽어옴 (PersistancePath)
        // 여기서 manifest가 최신인지 여부는 검증하지 않음, 단지 내가 현재 가지고있는 Manifest에 의해 게임이 정상적으로 구동될 수 있는지 여부만 검증함
        (IOResult readResult, string localJson) read = await rm.ReadAllTextsAsync(localManifestPath, ct);

        // 로컬 Manifest가 존재하는 경우 Resources에서 읽어온 Manifest보다 우선순위를 가짐
        // 이 과정중의 실패는 Default로 폴백되지만 로드되고 난 이후에는 폴백하지 않고 그대로 검증함
        if (read.readResult.succeed)
        {
            ContentRecord localRecord = null;
            try
            {
                localRecord = JsonSerializer.Deserialize<ContentRecord>(read.localJson, ContentJsonOptions.Options);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CMS] Local Manifest parse failed. Use default Manifest. reason={e.Message}");
            }
            if (localRecord != null &&
                    ContentRecordCodec.TryDecode(localRecord, out var localBody) &&
                    localBody is ContentManifest localManifest)
            {
                manifest = localManifest;
                manifestEntry.header = localRecord.header;
            }
        }


        // AssetMap에 Manifest 등록
        if (!rm.Register(manifest.staticKey, manifest, AccessMode.Public))
        {
            throw new InvalidOperationException(
                $"[GameIdentity.DefaultManifest.RegisterFailed] 기본 Manifest 등록에 실패했습니다. staticKey={manifest.staticKey}"
            );
        }
        
        ResourceManager.SetManifestStaticKey(manifest.staticKey);

        // 엔트리 등록
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

    /// <summary>
    /// 메타를 가진 컨텐츠 본문에 대한 검증
    /// 번들은 메타가 엔트리이기에 Verify로직을 타지 않음
    /// </summary>
    /// <param name="localMeta"></param>
    /// <param name="ctx"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
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

        if (ioResult != null)
        {
            ctx.httpResponseCode = ioResult.httpResponseCode;
        }

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

    private static async Task<IOResult> UpdateContentAsync(ContentEntry entry, ContentSyncContext ctx, CancellationToken ct = default)
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

        string localPath = ContentPath.GetContentLocalPath(entry);

        IOResult r = await rm.DownloadFileAsync(ctx.remoteMeta.dataUri, localPath, ct);
        if (r == null || !r.succeed)
        {
            return r ?? IOResult.Fail(IOFailReason.NetworkError);
        }

        ctx.dataUpdateSucceeded = true;
        return r;
    }

    private static async Task<IOResult> UpdateAddressablesCatalogAsync(AddressablesCatalogEntry entry, CancellationToken ct = default)
    {
        if (entry == null)
        {
            return IOResult.Fail(IOFailReason.InvalidResponse);
        }

        if (string.IsNullOrEmpty(entry.dataUri))
        {
            return IOResult.Fail(IOFailReason.InvalidResponse);
        }

        ResourceManager rm = ResourceManager.instance;

        if (rm == null)
        {
            return IOResult.Fail(IOFailReason.LoadFailed);
        }

        string localPath = ContentPath.GetContentLocalPath(entry);

        IOResult ir = await rm.DownloadFileAsync(entry.dataUri, localPath, ct);

        if (ir == null || !ir.succeed)
        {
            return ir ?? IOResult.Fail(IOFailReason.NetworkError);
        }

        return IOResult.Ok();
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

        string localPath = ContentPath.GetContentLocalPath(bundle);

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
        (ContentSyncContext ctx, ContentEntry entry) checkedContent =
            await CheckContentAsync(staticKey, ct);

        ContentSyncContext ctx = checkedContent.ctx;
        ContentEntry entry = checkedContent.entry;

        if (ctx.verifyResult == VerifyResult.UpToDate)
        {
            ctx.syncResult = SyncResult.UpToDate;
            return ctx;
        }

        if (ctx.verifyResult == VerifyResult.Failed)
        {
            ctx.syncResult = SyncResult.Failed;
            return ctx;
        }

        if (entry == null)
        {
            ctx.syncResult = SyncResult.Failed;
            ctx.failReason = SyncFailReason.NotFound;
            return ctx;
        }

        switch (entry)
        {
            case ContentBundleEntry bundleEntry:
                {
                    IOResult ir = await UpdateBundleAsync(bundleEntry, ct);

                    if (ir == null || !ir.succeed)
                    {
                        ctx.syncResult = SyncResult.Failed;
                        return ctx;
                    }

                    ctx.syncResult = SyncResult.Updated;
                    return ctx;
                }

            case AddressablesCatalogEntry addressablesCatalog:
                {
                    IOResult ir = await UpdateAddressablesCatalogAsync(addressablesCatalog, ct);

                    if (ir == null || !ir.succeed)
                    {
                        ctx.syncResult = SyncResult.Failed;
                        return ctx;
                    }

                    ctx.syncResult = SyncResult.Updated;
                    return ctx;
                }

            default:
                {
                    IOResult ir = await UpdateContentAsync(entry, ctx, ct);

                    if (ir == null || !ir.succeed)
                    {
                        ctx.syncResult = SyncResult.Failed;
                        return ctx;
                    }

                    ctx.syncResult = SyncResult.Updated;
                    return ctx;
                }
        }
    }

    private static async Task<ContentSyncContext> SyncAddressableCatalogAsync(AddressablesCatalogEntry addrEntry, ContentSyncContext ctx, CancellationToken ct = default)
    {
        ResourceManager rm = ResourceManager.instance;

        if (rm == null)
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.NotInitialized;
            ctx.syncResult = SyncResult.Failed;
            return ctx;
        }

        string payloadPath = ContentPath.GetContentLocalPath(addrEntry);

        ctx.verifyResult = VerifyResult.UpToDate;

        if (addrEntry.ownerStatickey == 0)
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.InvalidResponse;
            ctx.syncResult = SyncResult.Failed;
            return ctx;
        }

        IOResult existsIR = rm.Exists(payloadPath);

        if (!existsIR.succeed)
        {
            ctx.verifyResult = VerifyResult.Failed;

            switch (existsIR.failReason)
            {
                case IOFailReason.NotFound:
                    ctx.verifyResult = VerifyResult.Outdated;
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


        switch (ctx.verifyResult)
        {
            case VerifyResult.UpToDate:
                ctx.syncResult = SyncResult.UpToDate;
                break;
            case VerifyResult.Outdated:
                {
                    if (string.IsNullOrEmpty(addrEntry.dataUri))
                    {
                        ctx.verifyResult = VerifyResult.Failed;
                        ctx.failReason = SyncFailReason.InvalidResponse;
                        ctx.syncResult = SyncResult.Failed;
                        return ctx;
                    }

                    IOResult ir = await ResourceManager.instance.DownloadFileAsync(addrEntry.dataUri, payloadPath, ct);

                    if (ir == null || !ir.succeed)
                    {
                        ctx.syncResult = SyncResult.Failed;
                        return ctx;
                    }

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

    private static async Task<ContentSyncContext> SyncBundleAsync(ContentBundleEntry bundleEntry, ContentSyncContext ctx, CancellationToken ct = default)
    {
        string payloadPath = ContentPath.GetContentLocalPath(bundleEntry);

        // sizeBytes는 엔트리 규약상 반드시 유효값이어야 한다는 전제
        if (bundleEntry.sizeBytes <= 0)
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.InvalidResponse;
            ctx.syncResult = SyncResult.Failed;
            return ctx;
        }

        IOResult bundleIR = ResourceManager.instance.TryGetFileInfo(payloadPath, out FileInfo bundleInfo);

        ctx.verifyResult = VerifyResult.UpToDate;
        ctx.failReason = SyncFailReason.None;

        if (!bundleIR.succeed)
        {
            switch (bundleIR.failReason)
            {
                case IOFailReason.NotFound:
                    ctx.verifyResult = VerifyResult.Outdated;
                    break;

                case IOFailReason.InvalidPath:
                    ctx.verifyResult = VerifyResult.Failed;
                    ctx.failReason = SyncFailReason.InvalidPath;
                    break;

                case IOFailReason.AccessDenied:
                    ctx.verifyResult = VerifyResult.Failed;
                    ctx.failReason = SyncFailReason.AccessDenied;
                    break;

                default:
                    ctx.verifyResult = VerifyResult.Failed;
                    ctx.failReason = SyncFailReason.InvalidResponse;
                    break;
            }
        }

        if (bundleIR.succeed && bundleInfo == null)
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.InvalidResponse;
        }

        if (bundleIR.succeed && bundleInfo != null && bundleInfo.Length != bundleEntry.sizeBytes)
        {
            ctx.verifyResult = VerifyResult.Outdated;
        }

        switch (ctx.verifyResult)
        {
            case VerifyResult.UpToDate:
                ctx.syncResult = SyncResult.UpToDate;
                break;
            case VerifyResult.Outdated:
                {
                    IOResult ir = await UpdateBundleAsync(bundleEntry, ct);

                    if (!ir.succeed)
                    {
                        ctx.syncResult = SyncResult.Failed;
                        break;
                    }
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

    public static async Task<(ContentSyncContext ctx, ContentEntry entry)> CheckContentAsync(uint staticKey, CancellationToken ct = default)
    {
        ResourceManager rm = ResourceManager.instance;

        ContentSyncContext ctx = new ContentSyncContext();
        ContentEntry entry = null;

        ctx.Bind(staticKey, string.Empty, string.Empty, ContentCategory.None);

        if (rm == null)
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.NotInitialized;
            return (ctx, entry);
        }

        if (!rm.TryGetContentEntry(staticKey, out entry) || entry == null)
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.NotFound;
            return (ctx, entry);
        }

        ContentHeader header = entry.header;
        ctx.Bind(staticKey, header.id, header.schema, header.category);

        if (string.IsNullOrEmpty(header.id) || string.IsNullOrEmpty(header.schema))
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.InvalidPath;
            return (ctx, entry);
        }

        switch (entry)
        {
            case ContentBundleEntry bundleEntry:
                {
                    string payloadPath = ContentPath.GetContentLocalPath(bundleEntry);
                    IOResult bundleIR = rm.TryGetFileInfo(payloadPath, out FileInfo info);

                    if (!bundleIR.succeed)
                    {
                        if (bundleIR.failReason == IOFailReason.NotFound)
                        {
                            ctx.verifyResult = VerifyResult.Outdated;
                            return (ctx, entry);
                        }

                        ctx.verifyResult = VerifyResult.Failed;
                        ctx.failReason = SyncFailReason.InvalidResponse;
                        return (ctx, entry);
                    }

                    if (info == null)
                    {
                        ctx.verifyResult = VerifyResult.Failed;
                        ctx.failReason = SyncFailReason.InvalidResponse;
                        return (ctx, entry);
                    }

                    if (bundleEntry.sizeBytes > 0 && info.Length != bundleEntry.sizeBytes)
                    {
                        ctx.verifyResult = VerifyResult.Outdated;
                        return (ctx, entry);
                    }

                    ctx.verifyResult = VerifyResult.UpToDate;
                    return (ctx, entry);
                }

            case AddressablesCatalogEntry addrEntry:
                {
                    string payloadPath = ContentPath.GetContentLocalPath(addrEntry);
                    IOResult existsIR = rm.Exists(payloadPath);

                    if (existsIR.succeed)
                    {
                        ctx.verifyResult = VerifyResult.UpToDate;
                        return (ctx, entry);
                    }

                    if (existsIR.failReason == IOFailReason.NotFound)
                    {
                        ctx.verifyResult = VerifyResult.Outdated;
                        return (ctx, entry);
                    }

                    ctx.verifyResult = VerifyResult.Failed;
                    ctx.failReason = SyncFailReason.InvalidResponse;
                    return (ctx, entry);
                }

            default:
                break;
        }

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
            (IOResult readResult, string metaJson) read = await rm.ReadAllTextsAsync(localMetaPath, ct);

            if (read.readResult != null && read.readResult.succeed && !string.IsNullOrEmpty(read.metaJson))
            {
                try
                {
                    ContentRecord localMetaRecord =
                        JsonSerializer.Deserialize<ContentRecord>(read.metaJson, ContentRecordCodec.Options);

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

        await VerifyContentMetaAsync(localMeta, ctx, ct);

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
        }

        return (ctx, entry);
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

        string correctHash = string.Empty;
        ContentMeta localMeta = null;

        switch (entry)
        {
            case AddressablesCatalogEntry addrEntry:
                correctHash = addrEntry.sha256;
                break;
            case ContentBundleEntry bundleEntry:
                correctHash = bundleEntry.sha256;
                break;
            default:
                {
                    // 헤더에서 추출한 데이터를 통해 애셋의 로컬경로에 메타가 있는지를 확인함
                    string localMetaPath = Path.Combine(
                        Application.persistentDataPath,
                        ContentCategory.Meta.ToString(),
                        header.schema,
                        header.id + ".meta.json"
                    );

                    // 정합성 검증에서는 LocalMeta가 없거나 해시값이 비어있는 경우는 검증 실패로 간주함

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
                    correctHash = localMeta.sha256;
                }
                break;
        }

        if (string.IsNullOrEmpty(correctHash))
        {
            ctx.verifyResult = VerifyResult.Failed;
            ctx.failReason = SyncFailReason.InvalidResponse;
            ctx.syncResult = SyncResult.Failed;
            return ctx;
        }

        // 본문이 있는지를 확인하고 해시 계산을 준비
        string payloadPath = ContentPath.GetContentLocalPath(entry);

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
            
            if (!string.Equals(computedHash, correctHash, StringComparison.OrdinalIgnoreCase))
            {
                // 본문 해시 계산값과 메타 또는 엔트리에 적힌 해시값과 다름
                ctx.verifyResult = VerifyResult.Outdated;
                ctx.failReason = SyncFailReason.HashMismatch; 
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

// 메타는 외부에서 동기화 결과를 커밋할때 작업하도록 다시 고민중
// -->> 문제가 되는 상황 : 하위 엔트리 업데이트실패 & 로컬 폴백 업데이트 경로를 다르게 줄수는 없음 
//ContentRecord newMetaRecord = ContentRecordCodec.Encode<ContentMeta>(
//    staticKey,
//    header.id,
//    ctx.remoteMeta.version,
//    header.schema,
//    ContentCategory.Meta,
//    ctx.remoteMeta
//);

//string newMetaJson = JsonSerializer.Serialize(newMetaRecord, ContentRecordCodec.Options);
//await rm.SaveTextAsync(localMetaPath, newMetaJson, ct);