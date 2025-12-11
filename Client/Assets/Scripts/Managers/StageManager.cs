using UnityEngine;

public class StageManager : MonoBehaviour
{
    /// <summary>
    /// 씬에 따른 스테이지, 세부 스테이지 내부의 데이터를 관리하는 클래스
    /// </summary>
    public static StageManager instance;
    
    [SerializeField] private GameObject currentStageObject;
    [SerializeField] private int stageMainIndex;
    [SerializeField] private int stageSubIndex;
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

        stageMainIndex = 0;
        stageSubIndex = 0;
    }

    public void ChangeStage()
    {

    }
}
