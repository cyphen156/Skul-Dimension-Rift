using Assets.Scripts.Interface;
using System.Collections;
using UnityEngine;
using static Types;

public class Intro : MonoBehaviour, IPlayable
{
    // select background according to game Difficulty
    [SerializeField]
    private GameObject selectedBackground;
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

    private void Reset()
    {
        defaultBackground.SetActive(false);
        hardModeBackground.SetActive(false);
        neoWiz.SetActive(false);
        southPaw.SetActive(false);
    }

    #endregion Unity Methods

    #region Custom Methods
    IEnumerator IPlayable.C_Play()
    {
        Reset();
        // show background according to game mode
        GameDifficulty difficulty = GameManager.instance.GetGameMode();
        if (difficulty == GameDifficulty.Hard)
        {   bgm = "HardModeIntroBGM";
            selectedBackground = hardModeBackground;
        }
        else
        {
            bgm = "DefaultIntroBGM";
            selectedBackground = defaultBackground;
        }

        SoundManager.instance.PlayBGM(bgm);

        neoWiz.SetActive(true);
        yield return new WaitForSeconds(4.0f);
        neoWiz.SetActive(false);
        yield return new WaitForSeconds(0.2f);

        southPaw.SetActive(true);
        yield return new WaitForSeconds(4.0f);
        southPaw.SetActive(false);
        yield return new WaitForSeconds(0.2f);

        selectedBackground.SetActive(true);
        yield return new WaitForSeconds(4.0f);

        GameManager.instance.ChangeGameState(GameState.Ready);
    }
    #endregion Custom Methods
}
