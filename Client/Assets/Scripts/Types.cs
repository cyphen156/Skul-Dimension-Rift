
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
        Locked,
        Ready,      // 준비 상태 (예: 타이틀 화면)
        UIOnly,
        PlayerOnly,
        Restricted, // 제한된 플레이어 입력 (예: 대화 중)
    }
}