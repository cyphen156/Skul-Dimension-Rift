using System;
using System.Collections.Generic;
using UnityEngine;

public enum StageSpawnType
{
    None = 0,
    Player = 1,
    Monster = 2,
    WorldObject = 3,
    Gate = 4,
    Npc = 5
}

/// <summary>
/// 하나의 스테이지 프리팹(타일/벽 등) + 해당 스테이지 위에
/// 어떤 오브젝트들이 어떤 위치에 배치되는지에 대한 데이터.
/// </summary>
[Serializable]
public class StageSpawnEntry
{
    public StageSpawnType type;
    public uint objectKey;      // Monster / WorldObject / Npc / Gate 등에 사용
    public Vector3 position;    // 월드 좌표 (또는 로컬 좌표, 규칙 하나로 고정)
    public int weight;          // 선택형 게이트, 랜덤 스폰 등에 사용할 가중치(옵션)
}

/// <summary>
/// 특정 씬(SceneName) + (MainIndex, SubIndex) 조합으로 식별되는 스테이지 데이터.
/// 실제로는 JSON, ScriptableObject, DB 등에서 로드되어 들어올 예정.
/// </summary>
[Serializable]
public class StageData
{
    public string sceneName;
    public int mainIndex;
    public int subIndex;

    public string stagePrefabPath;     // 타일/벽 등을 가진 스테이지 프리팹 경로 (Addressables/Resource용)
    public List<StageSpawnEntry> spawns = new List<StageSpawnEntry>();
}
