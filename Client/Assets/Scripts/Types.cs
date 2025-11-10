using System;

/// <summary>
/// 정적 타입들을 정의하는 클래스
/// 게임 전반에 걸쳐 사용되는 열거형 타입들을 포함
/// </summary>
public static class Types
{
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
        Low,
        Middle,
        High,
    }
    #endregion UserData_Graphic

    #region UserData_Gameplay
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
        None,
        InGame,
    }
    #endregion UserData_Gameplay
    #endregion UserData Section
}