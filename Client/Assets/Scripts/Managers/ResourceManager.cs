using Assets.Scripts.Data;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.DebugUI.Table;
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

    [Header("Optional DLC Catalogs")]
    [SerializeField] private List<string> dlcCatalogUrls = new List<string>();

    [Header("Resources")]
    private Dictionary<string, AudioClip> bgmClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, Sprite> controlSprites = new Dictionary<string, Sprite>();

    [Header("UserDatas")]
    [SerializeField] private UserData userData;
    [SerializeField] private InputActionAsset userInputAsset;
#if UNITY_EDITOR
    [Header("DEBUG")]
    [SerializeField] private List<string> bgmKeys = new();
    [SerializeField] private List<string> sfxKeys = new();
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
        controlSprites.Clear();

        var sprites = Resources.LoadAll<Sprite>("Sprite/Controls");

#if UNITY_EDITOR
        bgmKeys.Clear();
        sfxKeys.Clear();
        bgmKeys.AddRange(bgmClips.Keys);
        sfxKeys.AddRange(sfxClips.Keys);
#endif
        userInputAsset = Resources.Load<InputActionAsset>(defaultInputActionPath);

        // 유저 데이터 로드

        userData = LoadUserData();
        if (userData == null)
        {
            var now = DateTime.UtcNow.ToString("o");

            userData = new UserData
            {
                createdAt = now,
                lastModified = now,
                control = new ControlData
                {
                    bindings = userInputAsset != null ? userInputAsset.ToJson() : "[]",
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
                    userInputAsset.LoadFromJson(userData.control.bindings);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ResourceManager] Failed to restore bindings: {e.Message}");
                }
            }
        }
        // 로드된 정보를 다른곳으로 뿌리는 작업 실행
        // 추후 게임 매니저가 직접 실행 
        #region refactor Section
        var options = userData.options;
        var graphic = options.graphic;
        var data = options.data;
        var audioData = options.audio;
        var gamePlayData = options.gameplay;
        SoundManager.instance.SetVolumes(audioData.masterVolume, audioData.BGMVolume, audioData.SFXVolume);

        Debug.Log("!");
        #endregion
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
            Debug.Log("UserData Save Successe");
            return true;
        }
        catch (Exception e)
        {
            Debug.Assert(true, $"Saveing UserData Failed : {e}");
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
            Debug.Log("User Data has been Loaded");
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
    #endregion
}