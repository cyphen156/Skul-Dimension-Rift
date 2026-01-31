using Assets.Scripts.Data;
using Assets.Scripts.Interface;
using Assets.Scripts.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 리소스 관리를 위한 매니저 클래스
///  - Get을 통해 적재된 리소스 반환
///  - Load / Unload는 CMS가 호출하고 RM이 수행
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager instance;

    [Header("ResourceManager - CMS internal Share Field")]
    private readonly object assetLoadLock = new object();
    //internal readonly Dictionary<Type, object> systemMaps = new Dictionary<Type, object>();
    internal Dictionary <uint, IResourceLocator> activelocators = new Dictionary<uint, IResourceLocator>(); // use addressables Catalog Locator

    internal Dictionary<uint, AsyncOperationHandle> activeStaticKeys = new Dictionary<uint, AsyncOperationHandle>();

    [Header("DomainProvider")]
    internal DomainAddressResolver domainAddressResolver; /// 리졸버에 한해 CMS에 직접 노출, 외부 접근 금지 ==> 아예 애셋 관리 시스템에서 제외
    internal HashSet<string> activeContents = new HashSet<string>();

    [Header("Content Asset Storage")]
    private readonly Dictionary<Type, object> assetMaps = new Dictionary<Type, object>();

    [SerializeField] private string defaultInputActionPath = "Input/InputActions";
  
    private const string userDataFileName = "UserData.json"; 
    private string userDataPath;

    private readonly Dictionary<string, string> controlPaths = new()
    {
        { "Keyboard&Mouse", "Control/Keyboard&Mouse" },
        //{ "Touch",          "Control/Touch" },
        { "Gamepad",        "Control/GamePad/General" },
        { "Gamepad_Xbox",   "Control/GamePad/Xbox" },
        { "Gamepad_PS",     "Control/GamePad/PlayStation" },
        { "Gamepad_Switch", "Control/GamePad/NintendoSwitch" },
    };

    [Header("Resources")]
    private Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    private Dictionary<string, AudioClip> bgmClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    private Dictionary<uint, StageData> stageDatas = new Dictionary<uint, StageData>();
    private readonly Dictionary<string, string> controlBindings = new Dictionary<string, string>();
    private readonly Dictionary<string, Sprite> controlSprites = new Dictionary<string, Sprite>();
    private Sprite placeHolderSprite;

    // ObjectKey 기반 스프라이트 캐시
    private readonly Dictionary<uint, Sprite> itemSprites = new Dictionary<uint, Sprite>();
    private readonly Dictionary<uint, Sprite> monsterSprites = new Dictionary<uint, Sprite>();
    private readonly Dictionary<uint, Sprite> worldObjectSprites = new Dictionary<uint, Sprite>();

    [Header("UserDatas")]
    [SerializeField] private UserData userData;
    [SerializeField] private InputActionAsset userInputAsset;

#if UNITY_EDITOR
    [Header("DEBUG")]
    [SerializeField] private List<string> prefabKeys = new List<string>();
    [SerializeField] private List<string> bgmKeys = new List<string>();
    [SerializeField] private List<string> sfxKeys = new List<string>();
    [SerializeField] private List<string> ControlSprites = new List<string>();
    [SerializeField] private List<string> ControlNames = new List<string>();
    [SerializeField] private List<string> spriteKeys = new List<string>();
#endif

    #region Unity Methods
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        userDataPath = Path.Combine(Application.persistentDataPath, userDataFileName);
        
        domainAddressResolver = new DomainAddressResolver();
#if UNITY_EDITOR
        List<DebugKeyValuePair> resolver = DebugUtility.ToDebugList(domainAddressResolver.Map);
#endif
        Initialize();
    }
    #endregion Unity Methods

    #region Initialization
    /// <summary>
    /// Resources에 존재하는 필수 데이터들 초기화
    /// </summary>
    private void Initialize()
    {
        // 시스템 리소스 로드
        prefabs.Clear();
        var Prefabs = Resources.LoadAll<GameObject>("Prefab");
        foreach (var pref in Prefabs)
        {
            prefabs[pref.name] = pref;
        }

        bgmClips.Clear();
        sfxClips.Clear();

        var AudioClips = Resources.LoadAll<AudioClip>("Audio");

        foreach (var clip in AudioClips)
        {
            if (clip.name.EndsWith("BGM"))
            {
                bgmClips[clip.name] = clip;
            }
            else if (clip.name.EndsWith("SFX"))
            {
                sfxClips[clip.name] = clip;
            }
        }

        placeHolderSprite = Resources.Load<Sprite>("Sprite/PlaceHolder");
#if UNITY_EDITOR
        prefabKeys.Clear();
        prefabKeys.AddRange(prefabs.Keys);
        bgmKeys.Clear();
        sfxKeys.Clear();
        bgmKeys.AddRange(bgmClips.Keys);
        sfxKeys.AddRange(sfxClips.Keys);
        ControlSprites.Clear();
        ControlNames.Clear();
        spriteKeys.Clear();
#endif
        userInputAsset = Resources.Load<InputActionAsset>(defaultInputActionPath);
        userInputAsset = Instantiate(userInputAsset);

        // 유저 데이터 로드
        userData = LoadUserData();
        if (userData == null)
        {
            var now = DateTime.UtcNow.ToString("o");
            InputActionMap playerMap = userInputAsset.FindActionMap("Player");
            userData = new UserData
            {
                createdAt = now,
                lastModified = now,
                control = new ControlData
                {
                    bindings = playerMap != null ? playerMap.SaveBindingOverridesAsJson() : ""
                }
            };
            SaveUserData();
        }
        else
        {
            if (!string.IsNullOrEmpty(userData.control.bindings) && userInputAsset != null)
            {
                try
                {
                    InputActionMap playerMap = userInputAsset.FindActionMap("Player");
                    if (playerMap != null)
                    {
                        InputActionRebindingExtensions.LoadBindingOverridesFromJson(playerMap, userData.control.bindings);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ResourceManager] Failed to restore bindings: {e.Message}");
                }
            }
        }

        // 컨트롤 이미지 불러오기
        controlSprites.Clear();
    }
    #endregion

    #region Regacy Resource Accessors 

    public StageData GetStageData(uint stageDataKey)
    {
        StageData data = null;
        stageDatas.TryGetValue(stageDataKey, out data);
        return data;
    }

    public InputActionAsset GetUserInputActions()
    {
        return userInputAsset;
    }

    public GameObject GetGameObject(string objectName)
    {
        GameObject gameObject;

        if (string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        if (!prefabs.TryGetValue(objectName, out gameObject))
        {
            gameObject = Resources.Load<GameObject>(objectName);
        }

        return gameObject;
    }
    
    public Sprite GetControlSprite(string controlName, bool isHighlight = false)
    {
        if (string.IsNullOrEmpty(controlName))
        {
            return null;
        }

        string bindingKey;
        if (!controlBindings.TryGetValue(controlName, out bindingKey))
        {
            return null;
        }

        if (string.IsNullOrEmpty(bindingKey))
        {
            return placeHolderSprite;
        }

        if (isHighlight)
        {
            bindingKey += "_White";
        }

        Sprite sprite;
        if (!controlSprites.TryGetValue(bindingKey, out sprite))
        {
            // 스프라이트를 조회했는데 없다면 에러처리한다.
            Debug.Log("there is No value In Dictionary");
        }

        return sprite;
    }

    public AudioClip GetBGMClip(string bgmClipName)
    {
        return bgmClips.ContainsKey(bgmClipName) ? bgmClips[bgmClipName] : null;
    }

    public AudioClip GetSFXClip(string sfxClipName)
    {
        return sfxClips.ContainsKey(sfxClipName) ? sfxClips[sfxClipName] : null;
    }

    public ref readonly UserData GetUserData()
    {
        return ref userData;
    }

    public OptionsData GetUserOptionsData()
    {
        return userData.options;
    }
    #endregion

    #region Regacy Resource Management 
    public bool SaveUserData()
    {
        try
        {
            // 현재 사용중인 데이터 저장 
            userData.lastModified = DateTime.UtcNow.ToString("o");
            InputActionMap playerMap = userInputAsset.FindActionMap("Player");

            userData.control.bindings = playerMap != null ? playerMap.SaveBindingOverridesAsJson() : "";
            var dir = Path.GetDirectoryName(userDataPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonUtility.ToJson(userData, prettyPrint: true);
            var temp = userDataPath + ".tmp";
            File.WriteAllText(temp, json);

            // 기존 파일이 있는 경우
            if (File.Exists(userDataPath))
            {
                var bak = userDataPath + ".bak";
                try { File.Copy(userDataPath, bak, true); } catch { /*백업 실패 무시*/ }
                File.Copy(temp, userDataPath, true);
                File.Delete(temp);
            }
            else
            {
                File.Move(temp, userDataPath);
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.Assert(false, $"Saving UserData Failed : {e}");
            return false;
        }
    }

    private UserData LoadUserData()
    {
        try
        {
            if (!File.Exists(userDataPath))
            {
                return null;
            }

            var json = File.ReadAllText(userDataPath);
            var loaded = JsonUtility.FromJson<UserData>(json);
            if (loaded == null)
            {
                throw new Exception("JSON parse failed");
            }
            return loaded;
        }
        catch (Exception e)
        {
            Debug.Log($"Loading UserData Failed : {e}");
            return null;
        }
    }

    public void ChangeResource(string dictionaryName, string ResourceTarget)
    {
        if (string.IsNullOrEmpty(dictionaryName) || string.IsNullOrEmpty(ResourceTarget))
        {
            Debug.LogWarning($"[ResourceManager] Invalid ChangeResource parameters: ({dictionaryName}, {ResourceTarget})");
            return;
        }

        switch (dictionaryName)
        {
            case "controlSprites":
                {
                    controlSprites.Clear();
                    controlPaths.TryGetValue(ResourceTarget, out var targetPath);
                    string path = "Sprite/" + targetPath;
                    /// 여기서 병목이 생긴다면 Load스크린 코루틴 띄워야 할 수도 있습니다
                    /// 스프라이트 150개 가량이라서 그럴것 같지는 않지만 혹시 몰라 주석달아놓기

                    // 게임 패드 종류라면 General폴더 먼저로드하기
                    // 이후 개별 패드 스프라이트중 동일한 것이 있다면 오버라이드
                    if (path.Contains("GamePad"))
                    {
                        string generalPath = "Sprite/" + controlPaths["Gamepad"];
                        var generalLoaded = Resources.LoadAll<Sprite>(generalPath);
                        for (int i = 0; i < generalLoaded.Length; i++)
                        {
                            var sprite = generalLoaded[i];
                            if (sprite == null)
                            {
                                continue;
                            }
                            controlSprites[sprite.name] = sprite;
                        }
                    }

                    var loaded = Resources.LoadAll<Sprite>(path);
                    if (loaded == null || loaded.Length == 0)
                    {
                        Debug.LogWarning($"[ResourceManager] No sprites found at: {path}");
                        return;
                    }

                    for (int i = 0; i < loaded.Length; i++)
                    {
                        var sprite = loaded[i];
                        if (sprite == null)
                        {
                            continue;
                        }
                        controlSprites[sprite.name] = sprite;
                    }
                }
                break;
            default:
                {
                }
                break;
        }
#if UNITY_EDITOR
        ControlSprites.Clear();
        ControlSprites.AddRange(controlSprites.Keys);
#endif
    }

    public void ApplyControlBinding(string controlName, string bindingKey)
    {
        if (string.IsNullOrEmpty(controlName))
        {
            return;
        }

        if (bindingKey == null)
        {
            bindingKey = string.Empty;
        }

        controlBindings[controlName] = bindingKey;
#if UNITY_EDITOR
        if (!ControlNames.Contains(controlName))
        {
            ControlNames.Add(controlName);
        }
        if (!spriteKeys.Contains(bindingKey))
        {
            spriteKeys.Add(bindingKey);
        }
#endif
    }

    public void ApplyOption(UIWidgetContainer widget)
    {
        if (widget == null)
        {
            return;
        }

        switch (widget.parentName)
        {
            case "DataReset":
                // 기존 데이터는 버리고 새 데이터 주입
                userData.options.data = new Data();
                break;
            case "CutScene":
                // 현재 동작하지 않을 버튼
                break;
            default:
                break;
        }
    }
    #endregion

    #region StreamContainer 
    /// <summary>
    /// 외부로 스트림을 노출시킬 스트림 컨테이너
    /// - 스트림의 생성/바인딩/정리는 RM만 가능
    /// - 외부는 Stream 사용
    /// - Dispose는 RM이 Task 완료 시점에 강제
    /// - Release를 통해 스트림 컨테이너를 조기 반환 가능
    /// </summary>
    public sealed class StreamContainer : IContainer
    {
        public bool Succeeded
        {
            get;
            private set;
        }

        public Stream Stream
        {
            get;
            private set;
        }

        /// <summary>
        /// 빌린 경로
        /// </summary>
        internal string LeasedPath
        {
            get;
            private set;
        }

        /// <summary>
        /// 빌려간 쪽 태그
        /// </summary>
        internal string OwnerTag
        {
            get;
            private set;
        }

        private int disposed;

        private StreamContainer()
        {
            Succeeded = false;
            Stream = null;
            LeasedPath = null;
            OwnerTag = null;
            disposed = 0;
        }

        internal static StreamContainer Create()
        {
            return new StreamContainer();
        }

        internal void Bind(Stream stream, string path, string ownerTag)
        {
            Stream = stream;
            LeasedPath = path;
            OwnerTag = ownerTag;
            Succeeded = (stream != null);
        }

        internal void Clear()
        {
            Succeeded = false;
            Stream = null;
            LeasedPath = null;
            OwnerTag = null;
        }

        internal bool TryDispose()
        {
            return Interlocked.Exchange(ref disposed, 1) == 0;
        }

        /// <summary>
        /// 외부 조기 반납
        /// </summary>
        public void Release()
        {
            ResourceManager.instance.Dispose(this);
        }
    }

    private struct LeaseRecord
    {
        public string OwnerTag;
        public long OpenedUtcTicks;
    }

    private readonly object streamLeaseLock = new object();
    private readonly HashSet<StreamContainer> streamContainers = new HashSet<StreamContainer>();
    private readonly Dictionary<string, LeaseRecord> leasedPaths = new Dictionary<string, LeaseRecord>();

    private bool denyDuplicatePathLease = true;

    private bool TryReservePath(string path, string ownerTag, out string reason)
    {
        reason = null;

        if (!denyDuplicatePathLease)
        {
            return true;
        }

        if (string.IsNullOrEmpty(path))
        {
            reason = "Invalid path.";
            return false;
        }

        LeaseRecord record;
        if (leasedPaths.TryGetValue(path, out record))
        {
            string owner = record.OwnerTag;
            if (string.IsNullOrEmpty(owner))
            {
                owner = "Unknown";
            }

            reason = $"Path already leased. owner={owner}";
            return false;
        }

        LeaseRecord newRecord = new LeaseRecord();
        newRecord.OwnerTag = string.IsNullOrEmpty(ownerTag) ? "Unknown" : ownerTag;
        newRecord.OpenedUtcTicks = DateTime.UtcNow.Ticks;

        leasedPaths[path] = newRecord;
        return true;
    }

    private void ReleaseReservedPath(string path)
    {
        if (!denyDuplicatePathLease)
        {
            return;
        }

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        leasedPaths.Remove(path);
    }

    private StreamContainer LeaseRead(string path, string ownerTag, out string reason)
    {
        reason = null;

        StreamContainer container = StreamContainer.Create();

        if (string.IsNullOrEmpty(path))
        {
            reason = "Invalid path.";
            return container;
        }

        if (File.Exists(path) == false)
        {
            reason = "File does not exist.";
            return container;
        }

        lock (streamLeaseLock)
        {
            if (!TryReservePath(path, ownerTag, out reason))
            {
                return container;
            }
        }

        try
        {
            FileStream fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                256 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            );

            container.Bind(fs, path, ownerTag);

            lock (streamLeaseLock)
            {
                streamContainers.Add(container);
            }

            return container;
        }
        catch (Exception e)
        {
            lock (streamLeaseLock)
            {
                ReleaseReservedPath(path);
            }

            container.Clear();
            reason = $"Open failed: {e.Message}";
            return container;
        }
    }

    /// <summary>
    /// Taks완료 시점에 스트림 컨테이너 정리 바인딩
    /// Task가 무한 루프를 도는 경우는 상위 로직에서 방지해야 함
    /// </summary>
    /// <param name="container"></param>
    /// <param name="work"></param>
    public void BindTask(StreamContainer container, Task work)
    {
        if (container == null)
        {
            return;
        }

        if (!container.Succeeded || work == null)
        {
            Dispose(container);
            return;
        }

        work.ContinueWith(
            OnWorkCompleted,
            container,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
    }

    private static void OnWorkCompleted(Task t, object state)
    {
        StreamContainer container = state as StreamContainer;
        if (container == null)
        {
            return;
        }

        ResourceManager.instance.Dispose(container);
    }

    private void Dispose(StreamContainer container)
    {
        if (container == null)
        {
            return;
        }

        if (!container.TryDispose())
        {
            return;
        }

        string leasedPath = container.LeasedPath;

        lock (streamLeaseLock)
        {
            streamContainers.Remove(container);
            ReleaseReservedPath(leasedPath);
        }

        Stream stream = container.Stream;

        if (stream != null)
        {
            try
            {
                stream.Dispose();
            }
            catch
            {
            }
        }

        container.Clear();
    }
    #endregion

    #region Resource Management IO
    /// <summary>
    /// 경로 스트림 대여
    /// 실패 시 container.Succeeded=false, reason에 사유가 들어갑니다.
    /// </summary>
    public bool TryGetStreamContainer(string path, string ownerTag, out StreamContainer container, out string reason)
    {
        container = LeaseRead(path, ownerTag, out reason);
        return container != null && container.Succeeded;
    }

    public StreamContainer GetStreamContainer(string path)
    {
        string reason;
        StreamContainer c = LeaseRead(path, "Unknown", out reason);
        return c;
    }

    public bool IsPathLeased(string path, out string ownerTag)
    {
        ownerTag = null;

        lock (streamLeaseLock)
        {
            LeaseRecord record;
            if (leasedPaths.TryGetValue(path, out record))
            {
                ownerTag = record.OwnerTag;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 파일을 직접 읽어서 호출자가 해석하도록 바이트 배열로 반환
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public async Task<(IOResult result, byte[] data)> ReadAllBytesAsync(string path, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(path))
        {
            return (IOResult.Fail(IOFailReason.InvalidPath), null);
        }

        try
        {
            ct.ThrowIfCancellationRequested();

            if (!File.Exists(path))
            {
                return (IOResult.Fail(IOFailReason.NotFound), null);
            }

            byte[] data = await File.ReadAllBytesAsync(path, ct);

            return (IOResult.Ok(), data);
        }
        catch (OperationCanceledException oce)
        {
            return (IOResult.Fail(IOFailReason.Canceled, oce), null);
        }
        catch (UnauthorizedAccessException uae)
        {
            return (IOResult.Fail(IOFailReason.AccessDenied, uae), null);
        }
        catch (FileNotFoundException fnf)
        {
            return (IOResult.Fail(IOFailReason.NotFound, fnf), null);
        }
        catch (DirectoryNotFoundException dnf)
        {
            return (IOResult.Fail(IOFailReason.NotFound, dnf), null);
        }
        catch (IOException ioe)
        {
            return (IOResult.Fail(IOFailReason.AccessDenied, ioe), null);
        }
        catch (Exception e)
        {
            return (IOResult.Fail(IOFailReason.Unknown, e), null);
        }
    }

    /// CMS가 애셋을 담을 맵을 할당
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal bool AllocateAssetMap<T>()
    {
        if (assetMaps.ContainsKey(typeof(T)))
        {
            return false;
        }
        assetMaps[typeof(T)] = new Dictionary<uint, T>();
        return true;
    }

    /// <summary>
    /// 애셋을 사용하기 위해 필요한 정보를 등록하는 함수
    /// ResolveMap, DataSet, Catalog 등
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="staticKey"></param>
    /// <returns></returns>
    /// <summary>
    internal bool Register<T>(uint staticKey, T asset)
    {
        if (asset == null)
        {
            return false;
        }
        /// 딕셔너리 유무 확인
        object boxed;
        if (!assetMaps.TryGetValue(typeof(T), out boxed))
        {
            return false;
        }

        Dictionary<uint, T> map = (Dictionary<uint, T>)boxed;

        map[staticKey] = asset;
        return true;
    }

    internal void UnRegister<T>(uint staticKey)
    {
        /// 딕셔너리 유무 확인
        object boxed;
        if (!assetMaps.TryGetValue(typeof(T), out boxed))
        {
            return;
        }

        Dictionary<uint, T> map = (Dictionary<uint, T>)boxed;

        map.Remove(staticKey);
    }

    /// <summary>
    /// 애셋을 리졸브맵과 어드레서블 시스템을 이용하여 비동기로 적재
    /// 실제 Get으로 다른 시스템이 가져가 사용할 수 있도록 준비
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    internal async Task<IOResult> LoadAssetAsync<T>(uint staticKey)
        where T : UnityEngine.Object
    {
        T loadedAsset;

        if (TryGetAsset<T>(staticKey, out loadedAsset))
        {
            return IOResult.Ok();
        }

        string resolvedPath;
        if (domainAddressResolver == null || domainAddressResolver.TryResolve(staticKey, out resolvedPath) == false)
        {
            return IOResult.Fail(reason: IOFailReason.InvalidPath);
        }

        AsyncOperationHandle handle;
        bool isOwner;

        lock (assetLoadLock)
        {
            if (activeStaticKeys.TryGetValue(staticKey, out handle))
            {
                isOwner = false;
            }
            else
            {
                AsyncOperationHandle<T> created = Addressables.LoadAssetAsync<T>(resolvedPath);
                handle = created;
                activeStaticKeys[staticKey] = handle;
                isOwner = true;
            }
        }

        try
        {
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                return IOResult.Fail(reason: IOFailReason.InvalidPath);
            }

            if (TryGetAsset<T>(staticKey, out loadedAsset))
            {
                return IOResult.Ok();
            }

            AsyncOperationHandle<T> typed = handle.Convert<T>();
            T asset = typed.Result;

            if (asset == null)
            {
                return IOResult.Fail(reason: IOFailReason.InvalidPath);
            }

            if (Register<T>(staticKey, asset) == false)
            {
                if (isOwner)
                {
                    Addressables.Release(typed);
                }

                return IOResult.Fail(reason: IOFailReason.SaveFailed);
            }

            return IOResult.Ok();
        }
        finally
        {
            if (isOwner)
            {
                lock (assetLoadLock)
                {
                    activeStaticKeys.Remove(staticKey);
                }

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }
            }
        }
    }

    internal IOResult UnloadAsset<T>(uint staticKey)
        where T : UnityEngine.Object
    {
        T asset;

        if (!TryGetAsset<T>(staticKey, out asset))
        {
            return IOResult.Fail(reason: IOFailReason.NotFound);
        }

        UnRegister<T>(staticKey);

        if (asset != null)
        {
            Addressables.Release(asset);
        }

        return IOResult.Ok();
    }

    internal void ForceGarbageCollecting()
    {
        GC.Collect();
        Resources.UnloadUnusedAssets();
    }

    internal async Task<IOResult> SaveAsync(string path, byte[] bytes, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(path))
        {
            return IOResult.Fail(IOFailReason.InvalidPath);
        }

        if (bytes == null)
        {
            return IOResult.Fail(IOFailReason.SaveFailed, new ArgumentNullException(nameof(bytes)));
        }

        string tempPath = path + ".tmp";

        try
        {
            ct.ThrowIfCancellationRequested();

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        catch (Exception e)
        {
            return IOResult.Fail(IOFailReason.InvalidPath, e);
        }

        try
        {
            ct.ThrowIfCancellationRequested();

            // temp cleanup
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }

            // write temp
            using (FileStream fs = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                256 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan
            ))
            {
                await fs.WriteAsync(bytes, 0, bytes.Length, ct);
                await fs.FlushAsync(ct);
            }

            // commit
            await Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tempPath, path);
            }, ct);

            return IOResult.Ok();
        }
        catch (OperationCanceledException oce)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }

            return IOResult.Fail(IOFailReason.Canceled, oce);
        }
        catch (UnauthorizedAccessException uae)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }

            return IOResult.Fail(IOFailReason.AccessDenied, uae);
        }
        catch (Exception e)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }

            return IOResult.Fail(IOFailReason.SaveFailed, e);
        }
    }

    internal async Task<IOResult> DeleteAsync(string path, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(path))
        {
            return IOResult.Fail(IOFailReason.InvalidPath);
        }

        try
        {
            ct.ThrowIfCancellationRequested();

            await Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }, ct);

            return IOResult.Ok();
        }
        catch (OperationCanceledException oce)
        {
            return IOResult.Fail(IOFailReason.Canceled, oce);
        }
        catch (UnauthorizedAccessException uae)
        {
            return IOResult.Fail(IOFailReason.AccessDenied, uae);
        }
        catch (Exception e)
        {
            return IOResult.Fail(IOFailReason.Unknown, e);
        }
    }

    /// <summary>
    /// 문서 사이즈가 작은 경우, 버퍼로 바로 다운로드
    /// 외부 시스템에서 직접 파싱하도록 바이트 배열로 반환
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<(IOResult result, byte[] data)> DownloadBufferAsync(string uri, CancellationToken ct = default)
    {
        byte[] data = null;

        if (string.IsNullOrEmpty(uri))
        {
            return (IOResult.Fail(IOFailReason.InvalidUri), data);
        }

        long httpCode = 0;

        try
        {
            using (UnityWebRequest req = UnityWebRequest.Get(uri))
            {
                req.downloadHandler = new DownloadHandlerBuffer();

                UnityWebRequestAsyncOperation op = req.SendWebRequest();

                while (!op.isDone)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                httpCode = req.responseCode;

                bool httpOk = (httpCode >= 200) && (httpCode < 300);
                if (req.result != UnityWebRequest.Result.Success || !httpOk)
                {
                    return (IOResult.Fail(IOFailReason.NetworkError, new Exception(req.error), httpCode), null);
                }

                data = req.downloadHandler.data;

                if (data == null || data.Length == 0)
                {
                    return (IOResult.Fail(IOFailReason.NetworkError, new Exception("Empty response body."), httpCode), data);
                }
            }
        }
        catch (OperationCanceledException oce)
        {
            return (IOResult.Fail(IOFailReason.Canceled, oce, httpCode), null);
        }
        catch (Exception e)
        {
            return (IOResult.Fail(IOFailReason.NetworkError, e, httpCode), null);
        }

        return (IOResult.Ok(), data);
    }

    /// <summary>
    /// 버퍼를 쓰지 않고 파일에 직접 다운로드
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="path"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    internal async Task<IOResult> DownloadFileAsync(string uri, string path, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(uri))
        {
            return IOResult.Fail(IOFailReason.InvalidUri);
        }

        if (string.IsNullOrEmpty(path))
        {
            return IOResult.Fail(IOFailReason.InvalidPath);
        }

        string tempPath = path + ".tmp";

        // ensure dir
        try
        {
            ct.ThrowIfCancellationRequested();

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        catch (Exception e)
        {
            return IOResult.Fail(IOFailReason.InvalidPath, e);
        }

        // temp cleanup
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (Exception e)
        {
            return IOResult.Fail(IOFailReason.AccessDenied, e);
        }

        long httpCode = 0;

        // download
        try
        {
            using (UnityWebRequest req = UnityWebRequest.Get(uri))
            {
                req.downloadHandler = new DownloadHandlerFile(tempPath);

                UnityWebRequestAsyncOperation op = req.SendWebRequest();

                while (!op.isDone)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                httpCode = req.responseCode;

                bool httpOk = (httpCode >= 200) && (httpCode < 300);
                if (req.result != UnityWebRequest.Result.Success || !httpOk)
                {
                    try
                    {
                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }
                    }
                    catch
                    {
                    }

                    return IOResult.Fail(IOFailReason.NetworkError, new Exception(req.error), httpCode);
                }
            }
        }
        catch (OperationCanceledException oce)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }

            return IOResult.Fail(IOFailReason.Canceled, oce);
        }
        catch (Exception e)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }

            return IOResult.Fail(IOFailReason.NetworkError, e, httpCode);
        }

        // commit
        try
        {
            await Task.Run(() =>
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(tempPath, path);
            }, ct);

            IOResult ok = IOResult.Ok();
            ok.httpResponseCode = httpCode;
            return ok;
        }
        catch (OperationCanceledException oce)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }

            return IOResult.Fail(IOFailReason.Canceled, oce, httpCode);
        }
        catch (UnauthorizedAccessException uae)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }

            return IOResult.Fail(IOFailReason.AccessDenied, uae, httpCode);
        }
        catch (Exception e)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
            }

            return IOResult.Fail(IOFailReason.SaveFailed, e, httpCode);
        }
    }
#endregion

    #region Content Access Methods
    /// <summary>
    /// 게임 시스템이 로딩된 애셋을 가져가는 함수
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="staticKey">DomainKey</param>
    /// <param name="asset">return To</param>
    /// <returns>if (!= T || null => default/null) Else than T value</returns>
    public bool TryGetAsset<T>(uint staticKey, out T asset)
    {
        asset = default;

        object boxed;
        if (!assetMaps.TryGetValue(typeof(T), out boxed))
        {
            return false;
        }

        Dictionary<uint, T> map = (Dictionary<uint, T>)boxed;
        return map.TryGetValue(staticKey, out asset);
    }
    #endregion
}