using Assets.Scripts.Interface;
using UnityEngine;
using static GameManager;

public class Intro : MonoBehaviour, IPlayable
{
    // select background according to game Difficulty
    [SerializeField]
    private GameObject defaultBackground;
    [SerializeField]
    private GameObject hardModeBackground;
    [SerializeField]
    private string bgm;

    // 난이도와 상관 없이 재생되는 배경
    [SerializeField]
    private GameObject neoWiz;
    [SerializeField]
    private GameObject southPaw;

    #region Unity Methods

    private void Awake()
    {
        defaultBackground = transform.Find("DefaultBackground").gameObject;
        hardModeBackground = transform.Find("HardModeBackground").gameObject;
        neoWiz = transform.Find("NeoWiz").gameObject;
        southPaw = transform.Find("SouthPaw").gameObject;
    }

    private void OnEnable()
    {
        defaultBackground.SetActive(false);
        hardModeBackground.SetActive(false);
        neoWiz.SetActive(false);
        southPaw.SetActive(false);
    }

    #endregion Unity Methods

    #region Custom Methods
    public void Play()
    {
        GameDifficulty gm = GameManager.instance.GetGameMode();
        if (gm == GameDifficulty.Hard)
        {
            hardModeBackground.SetActive(true);
            defaultBackground.SetActive(false);
            bgm = "HardModeIntroBGM";
        }
        else
        {
            defaultBackground.SetActive(true);
            hardModeBackground.SetActive(false);
            bgm = "DefaultIntroBGM";
        }
        SoundManager.instance.PlayBGM(bgm);
    }
    #endregion Custom Methods
}
