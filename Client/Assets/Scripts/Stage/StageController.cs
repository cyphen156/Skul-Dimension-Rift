using Assets.Scripts.Data;
using UnityEngine;

public class StageController : MonoBehaviour
{
    [Header("Stage Root")]
    [SerializeField] private Transform stageRoot;

    [Header("Runtime")]
    [SerializeField] private GameObject currentStageObject;

    private void Awake()
    {
        if (stageRoot == null)
        {
            stageRoot = transform;
        }
    }

    public void Apply(StageData data)
    {
        if (data == null)
        {
            Debug.LogError("[StageController] StageData is null.");
            return;
        }

        Clear();

        string prefabKey = "StageTitle_0";

        if (string.IsNullOrEmpty(prefabKey) == true)
        {
            Debug.LogError("[StageController] PrefabKey resolve failed. stageStaticId=" + DomainKey.ToHex8(data.stageStaticId));
            return;
        }

        if (ResourceManager.instance == null)
        {
            Debug.LogError("[StageController] ResourceManager is null.");
            return;
        }

        GameObject prefab = ResourceManager.instance.GetGameObject(prefabKey);

        if (prefab == null)
        {
            Debug.LogError("[StageController] Prefab not found. key=" + prefabKey);
            return;
        }

        currentStageObject = Instantiate(prefab);

        ApplyTransformInfo(currentStageObject.transform, data.stagePose);
        // 스폰은 objectKey 기반 리졸브가 준비되면 여기서 수행
        // 지금은 데이터 컨테이너로만 유지
    }

    private void Clear()
    {
        if (currentStageObject != null)
        {
            Destroy(currentStageObject);
            currentStageObject = null;
        }
    }

    private void ApplyTransformInfo(Transform target, TransformInfo info)
    {
        if (target == null)
        {
            return;
        }

        target.localPosition = info.position;
        target.localRotation = info.rotation;
        target.localScale = info.scale;
    }

    public static string ResolvePrefabKey(uint stageStaticId)
    {
        // 규약 예시:
        // Prefab/Stage/DLC{dlc}/Stage_{main}_{sub}
        // 이 문자열 규칙은 사용하시는 프리팹 배치/어드레서블 주소 체계에 맞게 확정하시면 됩니다.

        if (stageStaticId == 0u)
        {
            return null;
        }

        if (DomainKey.GetDomain(stageStaticId) != Domain.Scene)
        {
            return null;
        }

        if (DomainKey.GetRole(stageStaticId) != (byte)SceneRole.StageData)
        {
            return null;
        }

        byte dlcIndex = DomainKey.GetGrade(stageStaticId);
        byte clazz = DomainKey.GetClass(stageStaticId);

        byte mainIndex;
        byte subIndex;

        ClassCodec.Unpack(clazz, out mainIndex, out subIndex);

        return "Prefab/Stage/DLC"
            + dlcIndex.ToString()
            + "/Stage_"
            + mainIndex.ToString()
            + "_"
            + subIndex.ToString();
    }
}
