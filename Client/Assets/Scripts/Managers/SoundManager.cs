using Assets.Scripts.Data;
using System.Collections.Generic;
using UnityEngine;
using static Types;

/// <summary>
/// 게임 사운드를 관리하는 싱글톤 매니저 클래스입니다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    [SerializeField] private Audio userAudioData;

    [Header("Volumes (0.0 ~ 1.0)")]
    [SerializeField] private float masterVolume;
    [SerializeField] private float bgmVolume;
    [SerializeField] private float sfxVolume;

    [Header("Listener")]
    [SerializeField] private Transform listenerTransform;

    [Header("BGM State")]
    [SerializeField] private AudioSource bgmSource;

    [Header("SFX")]
    [SerializeField] private List<AudioSource> sfxPool;
    [SerializeField] private int sfxPoolSize;
    [SerializeField] private int currentSFXIndex;

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

        // BGM AudioSource 설정
        bgmSource = GetComponent<AudioSource>();

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0.0f;
        bgmSource.dopplerLevel = 0.0f;
        bgmSource.clip = null;
    }

    private void Start()
    {
        if (listenerTransform == null)
        {
            GameObject go = Camera.main != null ? Camera.main.gameObject : null;
            AudioListener listener = go.GetComponent<AudioListener>();
            if (listener != null)
            {
                listenerTransform = listener.transform;
            }
            else
            {
                Debug.LogWarning("SoundManager: AudioListener not found in the scene.");
            }
        }
    }
    #endregion Unity Methods    

    #region Custom Methods
    private void Initialize()
    {
        userAudioData = ResourceManager.instance.GetUserOptionsData().audio;

        SetVolumes(userAudioData.masterVolume, userAudioData.BGMVolume, userAudioData.SFXVolume);
    }

    #region Volume Control
    private void ApplyVolume(VolumeType volumeType)
    {
        switch (volumeType)
        {
            case VolumeType.Master:
                AudioListener.volume = masterVolume;
                break;
            case VolumeType.BGM:
                if (bgmSource != null && bgmSource.isPlaying)
                {
                    bgmSource.volume = masterVolume * bgmVolume;
                }
                break;
            case VolumeType.SFX:
                if (sfxPool == null)
                {
                    return;
                }

                float v = masterVolume * sfxVolume;

                foreach (var sfx in sfxPool)
                {
                    if (sfx != null)
                    {
                        sfx.volume = v;
                    }
                }
                break;
            default:
                break;
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = masterVolume;
        ApplyVolume(VolumeType.BGM);
        ApplyVolume(VolumeType.SFX);
        ApplyVolume(VolumeType.Master);
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        ApplyVolume(VolumeType.BGM);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolume(VolumeType.SFX);
    }
    #endregion Volume Control

    public void PlayBGM(string bgmName)
    {
        AudioClip bgmClip = ResourceManager.instance.GetBGMClip(bgmName);

        if (bgmClip == null)
        {
            Debug.LogError($"SoundManager: BGM '{bgmName}' not found in ResourceManager.");
            return;
        }

        if (bgmSource.isPlaying)
        {
            StopBGM();
        }

        bgmSource.clip = bgmClip;
        bgmSource.volume = masterVolume * bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public void PlaySFX(string sfxName, Transform target)
    {
        var sfxClip = ResourceManager.instance.GetSFXClip(sfxName);
        if (sfxClip == null)
        {
            Debug.LogError($"SoundManager: SFX '{sfxName}' not found in ResourceManager.");
            return;
        }
    }

    public void SetVolumes(float masterVolume, float BGMVolume, float SFXVolume)
    {
        SetMasterVolume(masterVolume);
        SetBGMVolume(BGMVolume);
        SetSFXVolume(SFXVolume);
    }

    public void ApplyOption(Widget widget)
    {
        if (widget == null)
        {
            return;
        }

        float value;

        if (widget.widget is SliderWidget)
        {
            SliderWidget sw = widget.widget as SliderWidget;
            if (sw != null)
            {
                value = sw.slider.value;

                VolumeType type;
                Types.volumeType.TryGetValue(widget.parentName, out type);
                switch (type)
                {
                    case VolumeType.Master:
                        userAudioData.masterVolume = value;
                        SetMasterVolume(userAudioData.masterVolume);
                        break;
                    case VolumeType.BGM:
                        userAudioData.BGMVolume = value;
                        SetBGMVolume(userAudioData.BGMVolume);
                        break;
                    case VolumeType.SFX:
                        userAudioData.SFXVolume = value;
                        SetSFXVolume(userAudioData.SFXVolume);
                        break;
                    default:
                        break;
                }
            }
        }
    }
    #endregion Custom Methods
}
