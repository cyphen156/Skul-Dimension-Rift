using Assets.Scripts.Content;
using Assets.Scripts.Data;
using Assets.Scripts.Interface;
using Assets.Scripts.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
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

    public static uint ManifestStaticKey { get; private set; }
    
    [Header("paths")]
    [SerializeField] private string defaultInputActionPath = "Input/InputActions";
    private const string userDataFileName = "UserData.json"; 
    private string userDataPath;

    [Header("DomainProvider")]
    internal DomainAddressResolver domainAddressResolver; /// 리졸버에 한해 CMS에 직접 노출, 외부 접근 금지 ==> 아예 애셋 관리 시스템에서 제외

    [Header("Content Asset Storage")]
    internal TypeMapContainer[] assetContainers = new TypeMapContainer[Enum.GetValues(typeof(AccessMode)).Length];

    private readonly Dictionary<string, string> controlPaths = new()
    {
        { "Keyboard&Mouse", "Control/Keyboard&Mouse" },
        //{ "Touch",          "Control/Touch" },
        { "Gamepad",        "Control/GamePad/General" },
        { "Gamepad_Xbox",   "Control/GamePad/Xbox" },
        { "Gamepad_PS",     "Control/GamePad/PlayStation" },
        { "Gamepad_Switch", "Control/GamePad/NintendoSwitch" },
    };
    private Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    private Dictionary<string, AudioClip> bgmClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    private Dictionary<uint, StageData> stageDatas = new Dictionary<uint, StageData>();
    private readonly Dictionary<string, string> controlBindings = new Dictionary<string, string>();
    private readonly Dictionary<string, Sprite> controlSprites = new Dictionary<string, Sprite>();
    private Sprite placeHolderSprite;
    [Header("UserDatas")]
    [SerializeField] private UserData userData;
    [SerializeField] private InputActionAsset userInputAsset;

#if UNITY_EDITOR
    [Header("DEBUG")]
    [SerializeField] private List<DebugKeyValuePair> debugAssetMaps = new List<DebugKeyValuePair>();
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

        // 애셋 컨테이너 초기화
        foreach (AccessMode mode in Enum.GetValues(typeof(AccessMode)))
        {
            assetContainers[(int)mode] = new TypeMapContainer(mode);
        }
    }

    internal static bool InitializeManifestStaticKey(uint staticKey)
    {
        if (staticKey == default)
        {
            return false;
        }

        if (ManifestStaticKey != default)
        {
            return false;
        }
        ManifestStaticKey = staticKey;
        return true;
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
    /// 텍스트 애셋을 읽어서 반환
    /// </summary>
    /// <param name="path"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<(IOResult result, string data)> ReadAllTextsAsync(string path, CancellationToken ct = default)
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

            string text = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);

            if (string.IsNullOrEmpty(text))
            {
                return (IOResult.Fail(IOFailReason.LoadFailed), null);
            }

            return (IOResult.Ok(), text);
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
            return (IOResult.Fail(IOFailReason.LoadFailed, ioe), null);
        }
        catch (Exception e)
        {
            return (IOResult.Fail(IOFailReason.Unknown, e), null);
        }
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
            return (IOResult.Fail(IOFailReason.Unknown, ioe), null);
        }
        catch (Exception e)
        {
            return (IOResult.Fail(IOFailReason.Unknown, e), null);
        }
    }
    /// <summary>
    /// CMS가 애셋을 담을 맵을 할당
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal bool AllocateTypeMap<T>(AccessMode mode)
    {
        TypeMapContainer container = assetContainers[(int)mode];
        /// 딕셔너리 유무 확인
        lock (container.LockObj)
        {   
            if (container.Maps.ContainsKey(typeof(T)))
            {
                return false;
            }
            container.Maps[typeof(T)] = new Dictionary<uint, T>();
            return true;
        }
    }

    /// <summary>
    /// 애셋을 사용하기 위해 필요한 정보를 등록하는 함수
    /// ResolveMap, DataSet, Catalog 등
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="staticKey"></param>
    /// <returns></returns>
    internal bool Register<T>(uint staticKey, T asset, AccessMode mode)
    {
        if (asset is null)
        {
            return false;
        }

        TypeMapContainer container = assetContainers[(int)mode];

        lock (container.LockObj)
        {
            if (!container.Maps.ContainsKey(typeof(T)))
            {
                if (!AllocateTypeMap<T>(mode))
                {
                    return false;
                }
            }

            if (!container.Maps.TryGetValue(typeof(T), out object boxed))
            {
                return false;
            }
            Dictionary<uint, T> map = (Dictionary<uint, T>)boxed;
            map[staticKey] = asset;
#if UNITY_EDITOR
        RefreshDebugRegisteredAssets();
#endif
            return true;
        }
    }

    internal void UnRegister<T>(uint staticKey, AccessMode mode)
    {
        TypeMapContainer container = assetContainers[(int)mode];
        lock (container.LockObj)
        {
            if (!container.Maps.TryGetValue(typeof(T), out object boxed))
            {
                return;
            }
            Dictionary<uint, T> map = (Dictionary<uint, T>)boxed;
            map.Remove(staticKey);
        }
#if UNITY_EDITOR
            RefreshDebugRegisteredAssets();
#endif
    }

    /// <summary>
    /// 애플리케이션에 기본적으로 내장되는 Resources로 호출되는 애셋 호출기
    /// </summary>
    /// <returns></returns>
    internal async Task<(IOResult result, T asset)> LoadDefaultAssetAsync<T>(string path, CancellationToken ct = default) 
        where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            return (IOResult.Fail(IOFailReason.InvalidPath), null);
        }

        ResourceRequest request;

        try
        {
            request = Resources.LoadAsync<T>(path);
        }
        catch (Exception e)
        {
            return (IOResult.Fail(IOFailReason.LoadFailed, e), null);
        }

        while (request.isDone == false)
        {
            if (ct.IsCancellationRequested)
            {
                return (IOResult.Fail(IOFailReason.Canceled), null);
            }

            await Task.Yield();
        }

        T loaded = request.asset as T;
        if (loaded == null)
        {
            return (IOResult.Fail(IOFailReason.NotFound), null);
        }

        return (IOResult.Ok(), loaded);
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
        string resolvedPath;
        if (domainAddressResolver == null || domainAddressResolver.TryResolve(staticKey, out resolvedPath) == false)
        {
            return IOResult.Fail(IOFailReason.InvalidPath);
        }

        TypeMapContainer internalContainer = assetContainers[(int)AccessMode.Internal];
        if (internalContainer == null)
        {
            return IOResult.Fail(IOFailReason.NotFound);
        }

        Task<IOResult> previousTask = null;
        TaskCompletionSource<IOResult> tcs =
            new TaskCompletionSource<IOResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<IOResult> enqueuedTask = tcs.Task;

        lock (internalContainer.LockObj)
        {
            TryGetAsset_Internal<Task<IOResult>>(staticKey, AccessMode.Internal, out previousTask);
            Register<Task<IOResult>>(staticKey, enqueuedTask, AccessMode.Internal);
        }

        if (previousTask != null)
        {
            try
            {
                await previousTask;
            }
            catch
            {
            }
        }

        IOResult result = IOResult.Ok();
        AsyncOperationHandle<T> typedHandle = default;
        bool isOwner = false;

        try
        {
            // 먼저 작업에서 애셋을 성공적으로 로드해서 공개처리되엇는가?
            if (TryGetAsset_Internal<T>(staticKey, AccessMode.Public, out _))
            {
                result = IOResult.Ok();
                return result;
            }

            // 아니라면 로드를 시작한다
            typedHandle = Addressables.LoadAssetAsync<T>(resolvedPath);
            isOwner = true;

            await typedHandle.Task;

            // 어드레서블 로드 실패
            if (typedHandle.Status != AsyncOperationStatus.Succeeded)
            {
                result = IOResult.Fail(IOFailReason.LoadFailed);
                return result;
            }

            // 로드된 애셋이 이상함
            T loaded = typedHandle.Result;
            if (loaded == null)
            {
                result = IOResult.Fail(IOFailReason.LoadFailed);
                return result;
            }

            // 공개된건 없는 데 기존 핸들이 남아있음
            if (TryGetAsset_Internal<AsyncOperationHandle>(staticKey, AccessMode.Internal, out var existingHandle))
            {
                // 기존 핸들을 해제하고
                UnRegister<AsyncOperationHandle>(staticKey, AccessMode.Internal);

                // 방출하기
                if (existingHandle.IsValid())
                {
                    Addressables.Release(existingHandle);
                }
            }

            // 재등록
            Register<AsyncOperationHandle>(staticKey, typedHandle, AccessMode.Internal);

            // 공개 등록 실패
            if (!Register<T>(staticKey, loaded, AccessMode.Public))
            {
                UnRegister<AsyncOperationHandle>(staticKey, AccessMode.Internal);
                Addressables.Release(typedHandle);
                result = IOResult.Fail(IOFailReason.RegistrationFailed);
                return result;
            }

            result = IOResult.Ok();
            return result;
        }
        catch (Exception e)
        {
            result = IOResult.Fail(IOFailReason.Unknown, e);
            return result;
        }
        finally
        {
            if (isOwner && typedHandle.IsValid() && typedHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(typedHandle);
            }

            tcs.TrySetResult(result);

            lock (internalContainer.LockObj)
            {
                if (TryGetAsset_Internal<Task<IOResult>>(staticKey, AccessMode.Internal, out var currentTail))
                {
                    if (object.ReferenceEquals(currentTail, enqueuedTask))
                    {
                        UnRegister<Task<IOResult>>(staticKey, AccessMode.Internal);
                    }
                }
            }
        }
    }

    internal async Task<IOResult> UnloadAssetAsync<T>(uint staticKey)
    where T : UnityEngine.Object
    {
        TypeMapContainer internalContainer = assetContainers[(int)AccessMode.Internal];
        if (internalContainer == null)
        {
            return IOResult.Fail(IOFailReason.NotFound);
        }

        Task<IOResult> previousTask = null;

        TaskCompletionSource<IOResult> tcs =
            new TaskCompletionSource<IOResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<IOResult> enqueuedTask = tcs.Task;

        lock (internalContainer.LockObj)
        {
            TryGetAsset_Internal<Task<IOResult>>(staticKey, AccessMode.Internal, out previousTask);
            Register<Task<IOResult>>(staticKey, enqueuedTask, AccessMode.Internal);
        }

        if (previousTask != null)
        {
            try
            {
                await previousTask;
            }
            catch
            {
            }
        }

        IOResult result = IOResult.Ok();

        try
        {
            UnRegister<T>(staticKey, AccessMode.Public);

            AsyncOperationHandle handle = default;
            bool hasHandle = false;

            lock (internalContainer.LockObj)
            {
                hasHandle = TryGetAsset_Internal<AsyncOperationHandle>(staticKey, AccessMode.Internal, out handle);

                if (hasHandle)
                {
                    UnRegister<AsyncOperationHandle>(staticKey, AccessMode.Internal);
                }
            }

            if (hasHandle && handle.IsValid())
            {
                Addressables.Release(handle);
            }

            result = IOResult.Ok();
            return result;
        }
        catch (Exception e)
        {
            result = IOResult.Fail(IOFailReason.Unknown, e);
            return result;
        }
        finally
        {
            tcs.TrySetResult(result);

            lock (internalContainer.LockObj)
            {
                if (TryGetAsset_Internal<Task<IOResult>>(staticKey, AccessMode.Internal, out var currentTail))
                {
                    if (object.ReferenceEquals(currentTail, enqueuedTask))
                    {
                        UnRegister<Task<IOResult>>(staticKey, AccessMode.Internal);
                    }
                }
            }
        }
    }

    internal void ForceGarbageCollecting()
    {
        GC.Collect();
        Resources.UnloadUnusedAssets();
    }

    public IOResult Exists(string path)
    {
        if (!File.Exists(path))
        {
            return IOResult.Fail(IOFailReason.NotFound);
        }
        return IOResult.Ok();
    }

    internal async Task<IOResult> SaveTextAsync(string path, string text, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(path))
        {
            return IOResult.Fail(IOFailReason.InvalidPath);
        }

        if (text == null)
        {
            return IOResult.Fail(IOFailReason.SaveFailed, new ArgumentNullException(nameof(text)));
        }

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return await SaveAsync(path, bytes, ct);
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
        catch (OperationCanceledException oce)
        {
            return IOResult.Fail(IOFailReason.Canceled, oce);
        }
        catch (Exception e)
        {
            return IOResult.Fail(IOFailReason.InvalidPath, e);
        }

        // temp cleanup
        try
        {
            ct.ThrowIfCancellationRequested();
            File.Delete(tempPath);
        }
        catch (OperationCanceledException oce)
        {
            return IOResult.Fail(IOFailReason.Canceled, oce);
        }
        catch (UnauthorizedAccessException uae)
        {
            return IOResult.Fail(IOFailReason.AccessDenied, uae);
        }
        catch (IOException ioe)
        {
            return IOResult.Fail(IOFailReason.AccessDenied, ioe);
        }
        catch (Exception e)
        {
            return IOResult.Fail(IOFailReason.InvalidPath, e);
        }

        // write temp
        try
        {
            ct.ThrowIfCancellationRequested();

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
        }
        catch (OperationCanceledException oce)
        {
            try
            {
                File.Delete(tempPath);
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
                File.Delete(tempPath);
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
                File.Delete(tempPath);
            }
            catch
            {
            }

            return IOResult.Fail(IOFailReason.SaveFailed, e);
        }

        // backUp
        string back = path + ".bak";
        bool isBackUped = false;

        if (File.Exists(path))
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    File.Delete(back);
                }
                catch
                {
                }

                File.Move(path, back);
                isBackUped = true;
            }
            catch (OperationCanceledException oce)
            {
                try
                {
                    File.Delete(tempPath);
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
                    File.Delete(tempPath);
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
                    File.Delete(tempPath);
                }
                catch
                {
                }

                return IOResult.Fail(IOFailReason.SaveFailed, e);
            }
        }

        IOResult result;

        // Commit
        try
        {
            ct.ThrowIfCancellationRequested();

            File.Move(tempPath, path);

            try
            {
                if (isBackUped)
                {
                    File.Delete(back);
                }
            }
            catch
            {
            }

            result = IOResult.Ok();
        }
        catch (OperationCanceledException oce)
        {
            try
            {
                if (isBackUped)
                {
                    File.Delete(path);
                    File.Move(back, path);
                }
            }
            catch
            {
            }

            result = IOResult.Fail(IOFailReason.Canceled, oce);
        }
        catch (UnauthorizedAccessException uae)
        {
            try
            {
                if (isBackUped)
                {
                    File.Delete(path);
                    File.Move(back, path);
                }
            }
            catch
            {
            }

            result = IOResult.Fail(IOFailReason.AccessDenied, uae);
        }
        catch (Exception e)
        {
            try
            {
                if (isBackUped)
                {
                    File.Delete(path);
                    File.Move(back, path);
                }
            }
            catch
            {
            }

            result = IOResult.Fail(IOFailReason.SaveFailed, e);
        }
        finally
        {
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
            }
        }

        return result;
    }

    internal Task<IOResult> Delete(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return Task.FromResult(IOResult.Fail(IOFailReason.InvalidPath));
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return Task.FromResult(IOResult.Ok());
        }
        catch (OperationCanceledException oce)
        {
            return Task.FromResult(IOResult.Fail(IOFailReason.Canceled, oce));
        }
        catch (UnauthorizedAccessException uae)
        {
            return Task.FromResult(IOResult.Fail(IOFailReason.AccessDenied, uae));
        }
        catch (Exception e)
        {
            return Task.FromResult(IOResult.Fail(IOFailReason.Unknown, e));
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
                    if (ct.IsCancellationRequested)
                    {
                        try
                        {
                            req.Abort();
                        }
                        catch
                        {
                        }

                        ct.ThrowIfCancellationRequested();
                    }

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

            IOResult ok = IOResult.Ok();
            ok.httpResponseCode = httpCode;
            return (ok, data);
        }
        catch (OperationCanceledException oce)
        {
            return (IOResult.Fail(IOFailReason.Canceled, oce, httpCode), null);
        }
        catch (Exception e)
        {
            return (IOResult.Fail(IOFailReason.NetworkError, e, httpCode), null);
        }
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
            ct.ThrowIfCancellationRequested();
            File.Delete(tempPath);
        }
        catch (OperationCanceledException oce)
        {
            return IOResult.Fail(IOFailReason.Canceled, oce);
        }
        catch (UnauthorizedAccessException uae)
        {
            return IOResult.Fail(IOFailReason.AccessDenied, uae);
        }
        catch (IOException ioe)
        {
            return IOResult.Fail(IOFailReason.AccessDenied, ioe);
        }
        catch (Exception e)
        {
            return IOResult.Fail(IOFailReason.InvalidPath, e);
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
                    if (ct.IsCancellationRequested)
                    {
                        try
                        {
                            req.Abort();
                        }
                        catch
                        {
                        }

                        ct.ThrowIfCancellationRequested();
                    }

                    await Task.Yield();
                }

                httpCode = req.responseCode;

                bool httpOk = (httpCode >= 200) && (httpCode < 300);
                if (req.result != UnityWebRequest.Result.Success || !httpOk)
                {
                    try
                    {
                        File.Delete(tempPath);
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
                File.Delete(tempPath);
            }
            catch
            {
            }

            return IOResult.Fail(IOFailReason.Canceled, oce, httpCode);
        }
        catch (Exception e)
        {
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
            }

            return IOResult.Fail(IOFailReason.NetworkError, e, httpCode);
        }

        // backUp
        string back = path + ".bak";
        bool isBackUped = false;

        if (File.Exists(path))
        {
            try
            {
                try
                {
                    File.Delete(back);
                }
                catch 
                {
                }

                File.Move(path, back);
                isBackUped = true;
            }
            // 백업 실패시 커밋 하지 않음
            catch (UnauthorizedAccessException uae)
            {
                try
                {
                    File.Delete(tempPath);
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
                    File.Delete(tempPath);
                }
                catch
                {
                }

                return IOResult.Fail(IOFailReason.SaveFailed, e, httpCode);
            }
        }

        IOResult result;

        // Commit
        try
        {
            File.Move(tempPath, path);

            try
            {
                if (isBackUped)
                {
                    File.Delete(back);
                }
            }
            catch
            {
            }

            result = IOResult.Ok();
            result.httpResponseCode = httpCode;
        }
        catch (UnauthorizedAccessException uae)
        {
            try
            {
                if (isBackUped)
                {
                    File.Delete(path);
                    File.Move(back, path);
                }
            }
            catch
            {
            }

            result = IOResult.Fail(IOFailReason.AccessDenied, uae, httpCode);
        }
        catch (Exception e)
        {
            try
            {
                if (isBackUped)
                {
                    File.Delete(path);
                    File.Move(back, path);
                }
            }
            catch
            {
            }

            result = IOResult.Fail(IOFailReason.SaveFailed, e, httpCode);
        }
        finally
        {
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
            }
        }

        return result;
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
        return TryGetAsset_Internal<T>(staticKey, AccessMode.Public, out asset);
    }

    internal bool TryGetAsset_Internal<T>(uint staticKey, AccessMode mode, out T asset)
    {
        asset = default;

        int index = (int)mode;

        TypeMapContainer container = assetContainers[index];
        if (container == null)
        {
            return false;
        }

        lock (container.LockObj)
        {
            object boxed;
            if (!container.Maps.TryGetValue(typeof(T), out boxed))
            {
                return false;
            }

            Dictionary<uint, T> map = (Dictionary<uint, T>)boxed;
            return map.TryGetValue(staticKey, out asset);
        }
    }
    #endregion

    #region DebugHelper
#if UNITY_EDITOR
    private void RefreshDebugRegisteredAssets()
    {
        debugAssetMaps.Clear();

        if (assetContainers == null)
        {
            return;
        }

        // 4개의 컨테이너
        for (int i = 0; i < assetContainers.Length; i++)
        {
            TypeMapContainer container = assetContainers[i];
            if (container == null)
            {
                continue;
            }

            lock (container.LockObj)
            {
                foreach (KeyValuePair<Type, object> typeEntry in container.Maps)
                {
                    Type assetType = typeEntry.Key;
                    object boxedMap = typeEntry.Value;

                    if (boxedMap == null)
                    {
                        continue;
                    }

                    IEnumerable enumerable = boxedMap as IEnumerable;
                    if (enumerable == null)
                    {
                        continue;
                    }

                    foreach (object entry in enumerable)
                    {
                        if (entry == null)
                        {
                            continue;
                        }

                        Type entryType = entry.GetType();
                        PropertyInfo keyProp = entryType.GetProperty("Key");
                        PropertyInfo valProp = entryType.GetProperty("Value");

                        object kObj = null;
                        object vObj = null;

                        if (keyProp != null)
                        {
                            kObj = keyProp.GetValue(entry);
                        }

                        if (valProp != null)
                        {
                            vObj = valProp.GetValue(entry);
                        }

                        string k = Formatter.ToDebugString(kObj);
                        string v = Formatter.ToDebugString(vObj);

                        DebugKeyValuePair kv = new DebugKeyValuePair(k, v);
                        debugAssetMaps.Add(kv);
                    }
                }
            }
        }
#endif
    }
    #endregion
}