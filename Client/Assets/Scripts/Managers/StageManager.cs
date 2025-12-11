using Assets.Scripts.Common;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    /// <summary>
    /// 씬에 따른 스테이지, 세부 스테이지 내부의 데이터를 관리하고
    /// ObjectSpawner / GameManager 를 이용해 실제로 오브젝트를 배치하는 클래스.
    /// </summary>
    public static StageManager instance;

    [Header("Current Stage Index")]
    [SerializeField] private string currentSceneName;
    [SerializeField] private int stageMainIndex;
    [SerializeField] private int stageSubIndex;

    [Header("Runtime State")]
    [SerializeField] private GameObject currentStagePrefabInstance;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

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

    public void SetSceneName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) == true)
        {
            return;
        }

        currentSceneName = sceneName;
    }

    public void SetStageIndex(int mainIndex, int subIndex)
    {
        stageMainIndex = mainIndex;
        stageSubIndex = subIndex;
    }

    /// <summary>
    /// 씬 로드가 끝난 이후에 호출되어,
    /// 현재 SceneName + (MainIndex, SubIndex)에 해당하는 StageData를 가져와
    /// 스테이지 프리팹 및 몬스터/오브젝트/게이트/플레이어를 배치합니다.
    /// </summary>
    public void ApplyStageSettings()
    {
        ClearCurrentStage();

        if (string.IsNullOrEmpty(currentSceneName) == true)
        {
            Debug.LogWarning("[StageManager] currentSceneName is empty.");
            return;
        }

        StageData data = StageDatabase.GetStageData(currentSceneName, stageMainIndex, stageSubIndex);

        if (data == null)
        {
            Debug.LogWarning("[StageManager] StageData is null. scene=" +
                             currentSceneName + " main=" + stageMainIndex + " sub=" + stageSubIndex);
            return;
        }

        // 1) 스테이지 프리팹 배치 (타일/벽 등)
        SpawnStagePrefab(data);

        // 2) 개별 스폰 엔트리 처리
        SpawnEntries(data);
    }

    private void SpawnStagePrefab(StageData data)
    {
        if (string.IsNullOrEmpty(data.stagePrefabPath) == true)
        {
            return;
        }

        if (ResourceManager.instance == null)
        {
            Debug.LogWarning("[StageManager] ResourceManager is null.");
            return;
        }

        GameObject prefab = ResourceManager.instance.GetGameObject(data.stagePrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[StageManager] Stage prefab not found : " + data.stagePrefabPath);
            return;
        }

        currentStagePrefabInstance = Instantiate(prefab);
    }

    private void SpawnEntries(StageData data)
    {
        if (data.spawns == null)
        {
            return;
        }

        if (ObjectSpawner.instance == null)
        {
            Debug.LogWarning("[StageManager] ObjectSpawner is null.");
            return;
        }

        foreach (StageSpawnEntry entry in data.spawns)
        {
            switch (entry.type)
            {
                case StageSpawnType.Player:
                    {
                        // 플레이어 스폰 포인트
                        if (GameManager.instance != null)
                        {
                            GameManager.instance.EnsureLocalPlayer(entry.position);
                        }
                        break;
                    }
                case StageSpawnType.Monster:
                case StageSpawnType.WorldObject:
                case StageSpawnType.Gate:
                case StageSpawnType.Npc:
                    {
                        // 공통적으로 ObjectKey + ObjectSpawner 사용
                        ViewObject view = ObjectSpawner.instance.Spawn(entry.objectKey, entry.position);
                        if (view != null)
                        {
                            spawnedObjects.Add(view.gameObject);
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// 현재 스테이지 프리팹 및 스폰된 오브젝트 정리.
    /// 풀을 사용할 경우에는 Despawn 호출로 교체해야 합니다.
    /// </summary>
    private void ClearCurrentStage()
    {
        if (currentStagePrefabInstance != null)
        {
            Destroy(currentStagePrefabInstance);
            currentStagePrefabInstance = null;
        }

        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            GameObject go = spawnedObjects[i];
            if (go == null)
            {
                continue;
            }

            // 지금은 단순 Destroy. 나중에는 PoolManager.Despawn으로 교체 예정.
            Destroy(go);
        }

        spawnedObjects.Clear();
    }

    /// <summary>
    /// 스테이지 클리어 후 다음 스테이지로 넘어갈 때,
    /// 메인/서브 인덱스만 갱신해두고 ApplyStageSettings 를 다시 호출하면 됩니다.
    /// </summary>
    public void ChangeStage(int nextMainIndex, int nextSubIndex)
    {
        SetStageIndex(nextMainIndex, nextSubIndex);
        ApplyStageSettings();
    }
}