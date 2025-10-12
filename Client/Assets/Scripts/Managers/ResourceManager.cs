using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 어드레서블 리소스 관리를 위한 매니저 클래스
/// 씬 단위로 로드되는 리소스들을 관리
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager instance;

    // 플랫폼별 빌드 파일이 다를 수 있으므로 리소스 경로는 조합으로 구성
    // 원격 리소스 경로 URL = server IP OR baseURL + Platform + Contents
    [SerializeField] private string baseURL = "Your Server URL Here";       // 서버 URL
    [SerializeField] private string contentPath = "Your Content Path Here"; // 컨텐츠 패스

    [Header("Optional DLC Catalogs")]
    [SerializeField] private List<string> dlcCatalogUrls = new List<string>();

    private Dictionary<string, AudioClip> bgmClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();

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
#endif
    }
    #endregion

    #region Resource Accessors
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