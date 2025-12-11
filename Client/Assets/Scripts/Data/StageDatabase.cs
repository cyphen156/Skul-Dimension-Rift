using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 이름 + (메인, 서브) 인덱스로 StageData 를 조회하는 임시 레이어.
/// 실제 구현은 JSON, ScriptableObject, 서버 DB 등으로 교체 예정.
/// </summary>
public static class StageDatabase
{
    // 임시 예시용 캐시. 실제로는 ResourceManager나 별도 DataManager에서 관리하게 될 예정.
    private static readonly Dictionary<string, StageData> cache = new Dictionary<string, StageData>();

    public static void RegisterStageData(StageData data)
    {
        if (data == null)
        {
            return;
        }

        string key = MakeKey(data.sceneName, data.mainIndex, data.subIndex);

        if (cache.ContainsKey(key) == true)
        {
            Debug.LogWarning("[StageDatabase] Duplicate StageData key : " + key);
            return;
        }

        cache[key] = data;
    }

    public static StageData GetStageData(string sceneName, int mainIndex, int subIndex)
    {
        string key = MakeKey(sceneName, mainIndex, subIndex);

        StageData data;
        if (cache.TryGetValue(key, out data) == true)
        {
            return data;
        }

        // 여기서 실제로는
        // - ResourceManager.instance.LoadStageData(key)
        // - JSON 로드
        // - ScriptableObject 로드
        // 등을 시도하도록 교체될 예정입니다.
        Debug.LogWarning("[StageDatabase] StageData not found : " + key);
        return null;
    }

    private static string MakeKey(string sceneName, int mainIndex, int subIndex)
    {
        if (string.IsNullOrEmpty(sceneName) == true)
        {
            sceneName = "Unknown";
        }

        return string.Format("{0}_{1}_{2}", sceneName, mainIndex, subIndex);
    }
}
