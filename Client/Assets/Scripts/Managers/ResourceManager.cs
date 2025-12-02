using Assets.Scripts.Data;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using static Types;
using Object = UnityEngine.Object;

/// <summary>
/// 어드레서블 리소스 관리를 위한 매니저 클래스
/// 씬 단위로 로드되는 리소스들을 관리
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager instance;

    [Header("Paths")]
    // 플랫폼별 빌드 파일이 다를 수 있으므로 리소스 경로는 조합으로 구성
    // 원격 리소스 경로 URL = server IP OR baseURL + Platform + Contents
    [SerializeField] private string baseURL = "Your Server URL Here";       // 서버 URL
    [SerializeField] private string contentPath = "Your Content Path Here"; // 컨텐츠 패스
    [SerializeField] private string defaultInputActionPath = "Input/InputActions";
    private const string userDataFileName = "UserData.json";
    private string userDataPath => Path.Combine(Application.persistentDataPath, userDataFileName);

    private readonly Dictionary<string, string> controlPaths = new()
    {
        { "Keyboard&Mouse", "Control/Keyboard&Mouse" },
        //{ "Touch",          "Control/Touch" },
        { "Gamepad",        "Control/GamePad/General" },
        { "Gamepad_Xbox",   "Control/GamePad/Xbox" },
        { "Gamepad_PS",     "Control/GamePad/PlayStation" },
        { "Gamepad_Switch", "Control/GamePad/NintendoSwitch" },
    };

    [Header("Optional DLC Catalogs")]
    [SerializeField] private List<string> dlcCatalogUrls = new List<string>();

    [Header("Resources")]
    private Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    private Dictionary<string, AudioClip> bgmClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, string> controlBindings = new Dictionary<string, string>();
    private readonly Dictionary<string, Sprite> controlSprites = new Dictionary<string, Sprite>();
    private Sprite placeHolderSprite;

    // ObjectKey 기반 스프라이트 캐시
    private readonly Dictionary<uint, Sprite> itemSprites = new Dictionary<uint, Sprite>();
    private readonly Dictionary<uint, Sprite> monsterSprites = new Dictionary<uint, Sprite>();
    private readonly Dictionary<uint, Sprite> worldObjectSprites = new Dictionary<uint, Sprite>();

    [Header("Runtime System Data")]
    [SerializeField] private string controlDeviceInfo;

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
        Initialize();
    }

    #endregion Unity Methods

    #region Initialization
    private void Initialize()
    {
        // 시스템 리소스 로드

        // 어드레서블 초기화
        Addressables.InitializeAsync().WaitForCompletion();

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

    #region Resource Accessors
    public T GetResource<T>(string resourceName) where T : Object
    {
        // how to Resource??
        if (string.IsNullOrEmpty(resourceName))
        {
            return null;
        }
        else
        {
            // return caching data in ResourceManager
            return (T)Resources.Load<T>(resourceName);
        }
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

    public Sprite GetSprite(uint objectKey)
    {
        if (objectKey == 0u)
        {
            return null;
        }

        ObjectDomain domain = ObjectKey.GetDomain(objectKey);

        switch (domain)
        {
            case ObjectDomain.Item:
                {
                    return GetItemSprite(objectKey);
                }
            case ObjectDomain.Monster:
                {
                    return GetMonsterSprite(objectKey);
                }
            case ObjectDomain.WorldObject:
                {
                    return GetWorldObjectSprite(objectKey);
                }
            default:
                {
                    return placeHolderSprite;
                }
        }
    }

    private Sprite GetItemSprite(uint objectKey)
    {
        Sprite sprite;

        if (itemSprites.TryGetValue(objectKey, out sprite) == true)
        {
            return sprite;
        }

        // 파일 네이밍 규칙은 프로젝트에 맞게 조정
        string hex = ObjectKey.ToHex8(objectKey);
        string path = "Sprite/Item/Item_" + hex;

        sprite = Resources.Load<Sprite>(path);

        if (sprite != null)
        {
            itemSprites[objectKey] = sprite;
        }
#if UNITY_EDITOR
        else
        {
            Debug.LogWarning("[ResourceManager] Item sprite not found at path: " + path + " (key: " + hex + ")");
        }
#endif
        return sprite;
    }

    private Sprite GetMonsterSprite(uint objectKey)
    {
        Sprite sprite;

        if (monsterSprites.TryGetValue(objectKey, out sprite) == true)
        {
            return sprite;
        }

        // 예) Sprite/Monster/Monster_FF000001
        string hex = ObjectKey.ToHex8(objectKey);
        string path = "Sprite/Monster/Monster_" + hex;

        sprite = Resources.Load<Sprite>(path);

        if (sprite != null)
        {
            monsterSprites[objectKey] = sprite;
        }
#if UNITY_EDITOR
        else
        {
            Debug.LogWarning("[ResourceManager] Monster sprite not found at path: " + path + " (key: " + hex + ")");
        }
#endif
        return sprite;
    }

    private Sprite GetWorldObjectSprite(uint objectKey)
    {
        Sprite sprite;

        if (worldObjectSprites.TryGetValue(objectKey, out sprite) == true)
        {
            return sprite;
        }

        // 예) Sprite/WorldObject/WorldObject_FF000001
        string hex = ObjectKey.ToHex8(objectKey);
        string path = "Sprite/WorldObject/WorldObject_" + hex;

        sprite = Resources.Load<Sprite>(path);

        if (sprite != null)
        {
            worldObjectSprites[objectKey] = sprite;
        }
#if UNITY_EDITOR
        else
        {
            Debug.LogWarning("[ResourceManager] WorldObject sprite not found at path: " + path + " (key: " + hex + ")");
        }
#endif
        return sprite;
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

    #region Resource Management
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
#if UNITY_EDITOR
            Debug.Log("UserData Save Success");
#endif
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
#if UNITY_EDITOR
            Debug.Log("User Data has been Loaded");
#endif
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
                    controlDeviceInfo = ResourceTarget;
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
    #endregion

    #region Utility Methods
    private void CacheClear(Exception e = null)
    {
        // 에러가 발생했으면 전체 캐시를 강제로 삭제
        if (e != null)
        {
            Debug.LogError($"ResourceManager Cache Clear Exception: {e.Message}");

            Caching.ClearCache();
        }

        // 이 외에는 사용하지 않는 캐시만 삭제
        Addressables.CleanBundleCache();
    }

    /// <summary>
    /// 플랫폼 정보를 정규화하여 리턴합니다.
    /// 어드레서블 데이터를 다운로드 할때 사용할 예정입니다.
    /// </summary>
    /// <returns></returns>
    private static string GetPlatformFolder()
    {
        return Application.platform switch
        {
            RuntimePlatform.Android => "ANDROID",
            RuntimePlatform.IPhonePlayer => "IOS",
            RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor => "WINDOWS",
            RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor => "OSX",
            RuntimePlatform.WebGLPlayer => "WEB",
            _ => Application.platform.ToString()
        };
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
}