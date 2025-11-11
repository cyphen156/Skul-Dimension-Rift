using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 정적 타입들을 정의하는 클래스
/// 게임 전반에 걸쳐 사용되는 열거형 타입들을 포함
/// </summary>
public static class Types
{
    public static readonly Dictionary<string, GraphicType> graphicType =
        new()
        {
            { "Resolution", GraphicType.Resolution },
            { "Window", GraphicType.Window },
            { "LightingEffect", GraphicType.LightingEffect },
            { "ParticlePerformance", GraphicType.ParticlePerformance },
            { "windowEarthQuakeEffect", GraphicType.windowEarthQuakeEffect },
            { "shakingEffect", GraphicType.shakingEffect },
        };
    
    public static readonly Dictionary<string, VolumeType> volumeType =
        new()
        {
            { "MasterVolume", VolumeType.Master },
            { "BGMVolume", VolumeType.BGM },
            { "SFXVolume", VolumeType.SFX },
        };

    public static readonly Dictionary<string, GamePlayDataType> gamePlayDataType = new()
        {
            { "Language", GamePlayDataType.Languages },
            { "RukiMode", GamePlayDataType.RukiMode },
            { "ShowTimer", GamePlayDataType.ShowTimer },
            { "ShowUIs", GamePlayDataType.ShowUIs },
        };

    public static Dictionary<Resolution, Vector2> resolutionMap = new()
    {
        { Resolution.UHD_4K, new Vector2(4096, 2160) }, // DCI 4K (≈17:9)
        { Resolution.UHD,    new Vector2(3840, 2160) }, // 4K UHD (16:9)
        { Resolution.WQHD,   new Vector2(3440, 1440) }, // 울트라와이드 QHD (21:9)
        { Resolution.QHD,    new Vector2(2560, 1440) }, // QHD (16:9)
        { Resolution.FHD,    new Vector2(1920, 1080) }, // Full HD (16:9)
        { Resolution.HD,     new Vector2(1280,  720) }, // HD (16:9)
        { Resolution.SD,     new Vector2( 640,  480) }, // SD(4:3)
        { Resolution.Custom, new Vector2(   0,    0) }, // 사용자 지정
    };

    public enum GameMode
    {
        Single,
        MultiplayerCoop,
        MultiplayerVersus
    }

    public enum GameDifficulty
    {
        Default,
        Hard
    }

    public enum GameState
    {
        None,
        Reset,
        Ready,
        Paused,
        Playing,
        Victory,
        GameOver,
        Loading
    }

    public enum VolumeType
    {
        Master,
        BGM,
        SFX
    }

    /// <summary>
    /// 입력 제어 열거형
    /// UI 및 플레이어 입력을 구분하여 처리
    /// 만약 둘다 허용한다면, UI가 우선순위를 가짐
    /// ==> 따로 정책 필요하지만 
    ///     스컬은 UI창이 오픈되면 
    ///     게임 플레이 로직이 멈추는 게임이라서
    ///     InputMode를 UIOnly로 바꾸는 것으로 해결
    /// </summary>
    public enum InputMode
    {
        None,
        Locked,
        Ready,      // 준비 상태 (예: 타이틀 화면)
        UIOnly,
        PlayerOnly,
        Restricted, // 제한된 플레이어 입력 (예: 대화 중)
    }

    #region UserData Section
    /// <summary>
    /// UserData에 저장되는 타입의 실 사용 유형
    /// </summary>
    public enum UserDataType
    {
        ControlData,
        OptionData,
    }

    public enum OptionDataType
    {
        None,
        Graphic,
        Data,
        Audio,
        GamePlay,
    }

    #region UserData_Graphic
    public enum GraphicType
    {
        Resolution,
        Window,
        LightingEffect,
        ParticlePerformance,
        windowEarthQuakeEffect,
        shakingEffect
    }
        public enum Resolution
        {
            // UHD_4K부터 QHD까지는 기기에 따른 제한 필요
            UHD_4K,
            UHD,
            WQHD,
            QHD,
            FHD,
            HD,
            SD,
            Custom
        }
        public enum Window
        {
            FullScreen, 
            Window,
            BorderlessFullScreen,
        }

        public enum LightingEffect
        {
            On,
            Off,
        }

        public enum ParticlePerformance
        {
            Off,
            Low,
            Middle,
            High,
        }
    #endregion UserData_Graphic

    #region UserData_Gameplay
    public enum GamePlayDataType
    {
        Languages,
        RukiMode,
        ShowTimer,
        ShowUIs
    }
        public enum Languages
        {
            Korean,
            English,
            Japanese,
        }

        public enum RukiMode
        {
            On,
            Off,
        }

        public enum ShowTimer
        {
            On,
            Off,
        }

        public enum ShowUIs
        {
            All,
            HudOff,
            None,
        }
    #endregion UserData_Gameplay
    #endregion UserData Section
}