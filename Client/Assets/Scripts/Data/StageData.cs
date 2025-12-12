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
    public uint objectKey;              // Domain/Role/Grade/Class 포함
    public TransformInfo transformInfo; // 트랜스폼 정보
    public int weight;                  // 옵션
}

[Serializable]
public class StageData
{
    public uint stageStaticId;
    public TransformInfo stagePose;
    public List<StageSpawnEntry> spawns = new List<StageSpawnEntry>();
}
