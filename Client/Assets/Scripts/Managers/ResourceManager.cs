using Assets.Scripts.Content;
using Assets.Scripts.Data;
using Assets.Scripts.Interface;
using Assets.Scripts.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

/// <summary>
/// 리소스 관리를 위한 매니저 클래스
///  - Get을 통해 적재된 리소스 반환
///  - Load / Unload는 CMS가 호출하고 RM이 수행
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager instance;

    [Header("ContentManifest")]
    private const string defaultContentManifestPath = "Data/Manifest/ContentManifest";
    private ContentManifest contentManifest;
    public ContentManifest ContentManifest => contentManifest;
    private ContentVerifyContext manifestVerifyContext; // Manifest 검증용 컨텍스트 -> 유지 이유 : RequiredOnBoot 옵션에 따른 지연 검증 처리
    public ContentVerifyContext ManifestVerifyContext => manifestVerifyContext;

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

    [Header("ContentPacks")]
    private readonly Dictionary<uint, SceneEntry> sceneEntries = new Dictionary<uint, SceneEntry>();
    private readonly Dictionary<string, CatalogState> catalogStates = new Dictionary<string, CatalogState>();

    [Header("DomainProvider")]
    [SerializeField] private DomainAddressResolver domainAddressResolver;
#if UNITY_EDITOR
    [SerializeField]
    private List<SerializableKeyValuePair> domainResolverDebugList
        = new List<SerializableKeyValuePair>();
#endif
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
        
        Initialize();
        
        domainAddressResolver = new DomainAddressResolver();
        uint titleStaticKey = DomainKey.GetStaticId(
            DomainKey.Make(
                Domain.Scene,
                0,
                (byte)SceneRole.StagePrefab,
                ClassCodec.Pack(0, 0),
                0
            )
        );
        
        domainAddressResolver.Register(titleStaticKey, "Prefab/StageTitle_0");
#if UNITY_EDITOR
        domainResolverDebugList = Serializer.ToDebugList<uint, string>(domainAddressResolver.Map);
#endif
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
        // 콘텐츠 매니페스트 로드 => PersistentDataPath 우선, 없으면 Resources 폴더에서 로드
        if (!LoadContentManifest(out contentManifest))
        {
            Debug.LogError("Error : ContentManifest Not Exists!");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            return;
        }
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
    /// - 생성/바인딩/정리는 RM만 가능
    /// - 외부는 Stream 사용
    /// - Dispose는 RM이 Task 완료 시점에 강제
    /// </summary>
    public sealed class StreamContainer : IContainer, IDisposable
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

        private int disposed;

        private StreamContainer()
        {
            Succeeded = false;
            Stream = null;
            disposed = 0;
        }

        internal static StreamContainer Create()
        {
            return new StreamContainer();
        }

        internal void Bind(Stream stream)
        {
            Stream = stream;
            Succeeded = (stream != null);
        }

        internal void Clear()
        {
            Succeeded = false;
            Stream = null;
        }

        internal bool TryDispose()
        {
            return Interlocked.Exchange(ref disposed, 1) == 0;
        }

        public void Dispose()
        {
            ResourceManager.instance.ForceDispose(this);
        }
    }

    private sealed class StreamWorkState
    {
        public StreamContainer Container;
    }

    private readonly object streamWorkLock = new object();
    private readonly HashSet<StreamContainer> streamContainers = new HashSet<StreamContainer>();

    private StreamContainer LeaseRead(string path)
    {
        StreamContainer container = StreamContainer.Create();

        if (string.IsNullOrEmpty(path))
        {
            return container;
        }

        if (File.Exists(path) == false)
        {
            return container;
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

            container.Bind(fs);

            lock (streamWorkLock)
            {
                streamContainers.Add(container);
            }

            return container;
        }
        catch
        {
            container.Clear();
            return container;
        }
    }

    public StreamContainer GetReadOnlyStreamContainer(string path)
    {
        return LeaseRead(path);
    }
    public void Bind(StreamContainer container, Task work)
    {
        if (container == null)
        {
            return;
        }

        if (work == null)
        {
            ForceDispose(container);
            return;
        }

        StreamWorkState state = new StreamWorkState();
        state.Container = container;

        work.ContinueWith(
            OnWorkCompleted,
            state,
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default
        );
    }

    private static void OnWorkCompleted(Task t, object state)
    {
        StreamWorkState s = state as StreamWorkState;
        if (s == null)
        {
            return;
        }

        StreamContainer c = s.Container;
        if (c == null)
        {
            return;
        }

        ResourceManager.instance.ForceDispose(c);
    }

    public void ForceDispose(StreamContainer container)
    {
        if (container == null)
        {
            return;
        }

        if (!container.TryDispose())
        {
            return;
        }

        lock (streamWorkLock)
        {
            streamContainers.Remove(container);
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
    /// <summary>
    /// 읽기 전용 스트림 컨테이너 획득
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public StreamContainer GetStreamContainer(string path)
    {
        return LeaseRead(path);
    }

#region Resource Management IO
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

    internal async Task<IOResult> DownloadAsync(string uri, string path, CancellationToken ct = default)
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

#region Content Methods
    public bool LoadContentManifest(out ContentManifest manifest)
    {
        manifest = LoadContentPayload<ContentManifest>("ContentManifest", "Manifest");

        if (manifest == null)
        {
            TextAsset defaultAsset = Resources.Load<TextAsset>(defaultContentManifestPath);

            if (defaultAsset == null)
            {
                return false;
            }

            manifest = JsonUtility.FromJson<ContentManifest>(defaultAsset.text);
        }

        return manifest != null;
    }

    public T LoadContentPayload<T>(string id, string schema) where T : class
    {
        string payloadPath = Path.Combine(
            Application.persistentDataPath,
            "Data",
            schema,
            id + ".json"
        );

        if (File.Exists(payloadPath) == false)
        {
            return default(T);
        }

        string json = File.ReadAllText(payloadPath);

        if (string.IsNullOrEmpty(json))
        {
            return default(T);
        }

        T local = JsonUtility.FromJson<T>(json);
        return local;
    }

    public bool LoadContentMeta(out ContentMeta meta, string id, string schema)
    {
        meta = null;

        string metaPath = Path.Combine(
                        Application.persistentDataPath,
                        "Meta",
                        schema,
                        id + ".meta.json"
                    );

        if (!File.Exists(metaPath))
        {
            return false;
        }

        string json = File.ReadAllText(metaPath);

        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        meta = JsonUtility.FromJson<ContentMeta>(json);
        return meta != null;
    }

    /// <summary>
    /// 컨텐츠 본문 저장
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="id"></param>
    /// <param name="schema"></param>
    /// <returns></returns>
    public bool SaveContentPayLoad<T>(T payLoad, string path)
    {
        try
        {
            if (payLoad == null)
            {
                return false;
            }
            string json = JsonUtility.ToJson(payLoad, true);
            string directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ResourceManager] SaveContentManifest failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 메타 파일 저장
    /// </summary>
    /// <param name="ctx"></param>
    /// <returns></returns>
    public bool SaveContentMeta(ContentVerifyContext ctx)
    {
        try
        {
            if (ctx == null || ctx.remoteMeta == null)
            {
                return false;
            }

            string json = JsonUtility.ToJson(ctx.remoteMeta, true);
            string metaPath = Path.Combine(
                Application.persistentDataPath,
                "Meta",
                ctx.targetSchema,
                ctx.targetId + ".meta.json"
            );

            string directory = Path.GetDirectoryName(metaPath);

            if (string.IsNullOrEmpty(directory) == false && Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(metaPath, json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ResourceManager] SaveContentMeta failed: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Addressable 애셋 로드
    /// </summary>
    /// <param name="id"></param>
    public IEnumerator C_LoadSceneData(uint sceneId)
    {
        yield return null;
    }
    #endregion
}