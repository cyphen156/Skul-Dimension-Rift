using Assets.Scripts.Data;
using Assets.Scripts.Utility;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    /// <summary>
    /// 씬에 따른 스테이지, 세부 스테이지 내부의 데이터를 관리
    /// </summary>
    public static StageManager instance;

    [Header("Current Scene Pack")]
    [SerializeField] private string currentSceneName;
    [SerializeField] private uint currentSceneStaticId;
    [SerializeField] private byte currentDlcIndex;

    [Header("Current Stage Progress")]
    [SerializeField] private int stageMainIndex;
    [SerializeField] private int stageSubIndex;

    [Header("Runtime References")]
    [SerializeField] private StageController stageController;
    [SerializeField] private StageData currentStageData;

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
        currentStageData = null;
    }

    public string GetCurrentSceneName()
    {
        return currentSceneName;
    }

    public void OnSceneLoaded(string sceneName, uint sceneStaticId)
    {
        currentSceneName = sceneName;
        currentSceneStaticId = sceneStaticId;
        currentDlcIndex = DomainKey.GetGrade(sceneStaticId);

        stageMainIndex = 0;
        stageSubIndex = 0;

        RequestStage(stageMainIndex, stageSubIndex);
    }

    public void RequestStage(int mainIndex, int subIndex)
    {
        stageMainIndex = mainIndex;
        stageSubIndex = subIndex;

        currentStageData = BuildStageData(stageMainIndex, stageSubIndex);

        if (currentStageData == null)
        {
            Debug.LogError("[StageManager] BuildStageData failed.");
            return;
        }
    }

    public void RequestNextSubStage()
    {
        stageSubIndex += 1;
        RequestStage(stageMainIndex, stageSubIndex);
    }

    private StageData BuildStageData(int mainIndex, int subIndex)
    {
        uint stageStaticKey = MakeStageStaticId(currentDlcIndex, mainIndex, subIndex);

        StageData data = TryGetStageData(stageStaticKey);

        if (data == null)
        {
            data = new StageData();
            data.spawns = new System.Collections.Generic.List<StageSpawnEntry>();
        }

        data.stageStaticKey = stageStaticKey;

        if (data.spawns == null)
        {
            data.spawns = new System.Collections.Generic.List<StageSpawnEntry>();
        }

        return data;
    }

    private StageData TryGetStageData(uint stageStaticKey)
    {
        if (ResourceManager.instance == null)
        {
            return null;
        }

        try
        {
            return ResourceManager.instance.GetStageData(stageStaticKey);
        }
        catch
        {
            return null;
        }
    }

    private uint MakeStageStaticId(byte dlcIndex, int mainIndex, int subIndex)
    {
        byte clazz = ClassCodec.Pack((byte)mainIndex, (byte)subIndex);

        return DomainKey.GetStaticId(
            DomainKey.Make(Domain.Scene, dlcIndex, (byte)SceneRole.StageData, clazz, 0)
        );
    }
}