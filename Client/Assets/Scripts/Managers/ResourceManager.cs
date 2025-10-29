using Assets.Scripts.Data;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
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
    private Dictionary<string, AudioClip> bgmClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, string> controlBindings = new();
    private readonly Dictionary<string, Sprite> controlSprites = new();

    [Header("Runtime System Data")]
    [SerializeField] private string controlDeviceInfo;

    [Header("UserDatas")]
    [SerializeField] private UserData userData;
    [SerializeField] private InputActionAsset userInputAsset;

#if UNITY_EDITOR
    [Header("DEBUG")]
    [SerializeField] private List<string> bgmKeys = new();
    [SerializeField] private List<string> sfxKeys = new();
    [SerializeField] private List<string> controlKeys = new();
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

#if UNITY_EDITOR
        bgmKeys.Clear();
        sfxKeys.Clear();
        bgmKeys.AddRange(bgmClips.Keys);
        sfxKeys.AddRange(sfxClips.Keys);
        controlKeys.Clear();
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

        // 로드된 정보를 다른곳으로 뿌리는 작업 실행
        // 추후 게임 매니저가 직접 실행 
        #region refactor Section
        var options = userData.options;
        var graphic = options.graphic;
        var data = options.data;
        var audioData = options.audio;
        var gamePlayData = options.gameplay;
        //SoundManager.instance.SetVolumes(audioData.masterVolume, audioData.BGMVolume, audioData.SFXVolume);
        #endregion
    }

    #endregion

    #region Resource Accessors
    public void SetBindingInfo()
    {

    }
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

    public Sprite GetControlSprite(string key, bool isHighlight = false)
    {
        if (isHighlight)
        {
            key += "_White";
        }
        if (key != null && controlSprites.TryGetValue(key, out Sprite sprite))
        {
            return sprite;
        }

        return null;
    }
   
    public AudioClip GetBGMClip(string bgmClipName)
    {
        return bgmClips.ContainsKey(bgmClipName) ? bgmClips[bgmClipName] : null;
    }

    public AudioClip GetSFXClip(string sfxClipName)
    {
        return sfxClips.ContainsKey(sfxClipName) ? sfxClips[sfxClipName] : null;
    }
    #endregion

    #region Resource Management
    public bool SaveUserData()
    {
        try
        {
            // 현재 사용중인 데이터 저장 
            userData.lastModified = DateTime.UtcNow.ToString("o");

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
                File.Replace(temp, userDataPath, userDataPath + ".bak");
            }
            // 없는 경우 생성
            else
            {
                File.Move(temp, userDataPath);
            }
#if UNITY_EDITOR
            Debug.Log("UserData Save Successe");
#endif
            return true;
        }
        catch (Exception e)
        {
            Debug.Assert(false, $"Saveing UserData Failed : {e}");
            return false;
        }
    }

    private UserData LoadUserData()
    {
        try
        {
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
        controlKeys.Clear();
        controlKeys.AddRange(controlSprites.Keys);
#endif
    }
    #endregion
}