using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct TransformInfo
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

[Serializable]
public class StageSpawnEntry
{
    public uint objectStaticKey;                // Domain/Role/Grade/Class 포함
    public TransformInfo placementTransform;    // 배치되는 트랜스폼 정보
    public int weight;                          // 옵션
}

[Serializable]
public class StageData
{
    public uint stageStaticKey;
    public TransformInfo stageRootTransform;
    public List<StageSpawnEntry> spawns = new List<StageSpawnEntry>();
}
