using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using static Types;

/// <summary>
/// 어드레서블 리소스 관리를 위한 매니저 클래스
/// 씬 단위로 로드되는 리소스들을 관리
/// 씬 내부 리소스는 각 씬에서 관리
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager instance;


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
        }

        Initialize();
    }
    #endregion Unity Methods


    #region Custom Methods
    private void Initialize()
    {
        bgmClips.Clear();
        sfxClips.Clear();

        var AudioClips = Resources.LoadAll<AudioClip>("Audio");

        foreach (var clip in AudioClips)
        {
            if (clip.name.EndsWith("BGM"))
            {
                bgmClips[clip.name] = clip;
            }
            else if (clip.name.StartsWith("SFX"))
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

    public AudioClip GetBGMClip(string bgmClipName)
    {
        // 내부 
        return bgmClips.ContainsKey(bgmClipName) ? bgmClips[bgmClipName] : null;
    }

    public AudioClip GetSFXClip(string sfxClipName)
    {
        // 내부 
        return sfxClips.ContainsKey(sfxClipName) ? sfxClips[sfxClipName] : null;
    }
    #endregion
}
