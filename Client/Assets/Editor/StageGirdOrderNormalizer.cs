#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Text;

public class StageGridNormalizerWindow : EditorWindow
{
    private Object targetObject;

    // 카운트
    private int tilemapChanged;
    private int tilemapUnchanged;

    private int spriteChanged;
    private int spriteUnchanged;

    private int staticChanged;
    private int staticUnchanged;

    private int failureCount;

    // 로그 버퍼
    private StringBuilder failureLog = new StringBuilder();
    private StringBuilder changedTilemapLog = new StringBuilder();
    private StringBuilder changedSpriteLog = new StringBuilder();
    private StringBuilder changedStaticLog = new StringBuilder();

    [MenuItem("Tools/Stage/Stage Grid Normalizer")]
    public static void Open()
    {
        GetWindow<StageGridNormalizerWindow>("Stage Grid Normalizer");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Stage Grid Normalizer", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        targetObject = EditorGUILayout.ObjectField(
            "Target (Prefab or Scene Object)",
            targetObject,
            typeof(Object),
            true
        );

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Normalize"))
        {
            Normalize();
        }
    }

    private void Normalize()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("[StageNormalizer] 대상 오브젝트가 없습니다.");
            return;
        }

        ResetCounters();

        GameObject go = null;
        string path = null;

        // 씬 오브젝트
        if (targetObject is GameObject)
        {
            go = targetObject as GameObject;
        }
        // 프리팹 에셋
        else if (PrefabUtility.GetPrefabAssetType(targetObject) != PrefabAssetType.NotAPrefab)
        {
            path = AssetDatabase.GetAssetPath(targetObject);
            go = PrefabUtility.LoadPrefabContents(path);
        }

        if (go == null)
        {
            Debug.LogError("[StageNormalizer] 대상이 유효한 Prefab/Scene 오브젝트가 아닙니다.");
            return;
        }

        Grid grid = go.GetComponentInChildren<Grid>();
        if (grid == null)
        {
            Debug.LogError("[StageNormalizer] Grid를 찾을 수 없습니다.");
            return;
        }

        // 타일맵별 규칙 적용
        ApplyToTilemap(grid.transform, "Tilemap_Background_UnMove", "Background_UnMove");
        ApplyToTilemap(grid.transform, "Tilemap_Background", "Background");
        ApplyToTilemap(grid.transform, "Tilemap_Background_Override", "Background_Override");
        ApplyToTilemap(grid.transform, "Tilemap_Wall", "Wall");
        ApplyToTilemap(grid.transform, "Tilemap_Ground_UnMove", "Ground_UnMove");
        ApplyToTilemap(grid.transform, "Tilemap_Ground", "Ground");
        ApplyToTilemap(grid.transform, "Tilemap_Platform", "Platform");
        ApplyToTilemap(grid.transform, "Tilemap_Foreground_UnMove", "Foreground_UnMove");
        ApplyToTilemap(grid.transform, "Tilemap_Foreground", "Foreground");

        // 프리팹이면 저장
        if (path != null)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            PrefabUtility.UnloadPrefabContents(go);
        }

        PrintSummary();
    }

    private void ResetCounters()
    {
        tilemapChanged = 0;
        tilemapUnchanged = 0;

        spriteChanged = 0;
        spriteUnchanged = 0;

        staticChanged = 0;
        staticUnchanged = 0;

        failureCount = 0;

        failureLog.Length = 0;
        changedTilemapLog.Length = 0;
        changedSpriteLog.Length = 0;
        changedStaticLog.Length = 0;
    }

    private void ApplyToTilemap(Transform gridRoot, string tilemapName, string sortingLayer)
    {
        Transform tilemapTransform = gridRoot.Find(tilemapName);
        if (tilemapTransform == null)
        {
            AppendFailure(tilemapName + " (Tilemap 오브젝트 없음)", null);
            return;
        }

        GameObject tilemapObject = tilemapTransform.gameObject;

        // TilemapRenderer 정규화
        TilemapRenderer tileRenderer = tilemapObject.GetComponent<TilemapRenderer>();
        if (tileRenderer != null)
        {
            string oldLayer = tileRenderer.sortingLayerName;
            int oldOrder = tileRenderer.sortingOrder;

            bool sameLayer = oldLayer == sortingLayer;
            bool sameOrder = oldOrder == 0;

            if (sameLayer == true && sameOrder == true)
            {
                tilemapUnchanged++;
            }
            else
            {
                changedTilemapLog.AppendLine(
                    "  * " + tilemapName +
                    " : Layer " + oldLayer + " → " + sortingLayer +
                    ", Order " + oldOrder + " → 0"
                );

                tileRenderer.sortingLayerName = sortingLayer;
                tileRenderer.sortingOrder = 0;
                tilemapChanged++;
            }
        }
        else
        {
            AppendFailure("TilemapRenderer 없음", tilemapTransform);
        }

        // Order_* 그룹 처리
        int childCount = tilemapTransform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = tilemapTransform.GetChild(i);

            if (child.name.StartsWith("Order_") == false)
            {
                continue;
            }

            int orderIndex = ParseOrderIndex(child.name);
            ApplySpriteRenderersRecursive(child, sortingLayer, orderIndex);
        }

        // Static 플래그 전파 (부모 Tilemap 기준)
        ApplyStaticFromParentRecursive(tilemapTransform);
    }

    private int ParseOrderIndex(string name)
    {
        int underscoreIndex = name.IndexOf('_');
        if (underscoreIndex < 0 || underscoreIndex + 1 >= name.Length)
        {
            return 0;
        }

        string numberPart = name.Substring(underscoreIndex + 1);
        int parsed;
        if (int.TryParse(numberPart, out parsed) == true)
        {
            return parsed;
        }

        return 0;
    }

    private void ApplySpriteRenderersRecursive(Transform root, string sortingLayer, int orderInLayer)
    {
        SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            string oldLayer = sr.sortingLayerName;
            int oldOrder = sr.sortingOrder;

            bool sameLayer = oldLayer == sortingLayer;
            bool sameOrder = oldOrder == orderInLayer;

            if (sameLayer == true && sameOrder == true)
            {
                spriteUnchanged++;
            }
            else
            {
                changedSpriteLog.AppendLine(
                    "  * " + GetFullPath(root) +
                    " : Layer " + oldLayer + " → " + sortingLayer +
                    ", Order " + oldOrder + " → " + orderInLayer
                );

                sr.sortingLayerName = sortingLayer;
                sr.sortingOrder = orderInLayer;
                spriteChanged++;
            }
        }

        int childCount = root.childCount;
        for (int i = 0; i < childCount; i++)
        {
            ApplySpriteRenderersRecursive(root.GetChild(i), sortingLayer, orderInLayer);
        }
    }

    // 부모 Tilemap의 Static 플래그를 자식에게 그대로 복사
    private void ApplyStaticFromParentRecursive(Transform parent)
    {
        GameObject parentObj = parent.gameObject;
        StaticEditorFlags parentFlags = GameObjectUtility.GetStaticEditorFlags(parentObj);

        int childCount = parent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = parent.GetChild(i);
            GameObject childObj = child.gameObject;

            StaticEditorFlags childFlags = GameObjectUtility.GetStaticEditorFlags(childObj);

            if (childFlags == parentFlags)
            {
                staticUnchanged++;
            }
            else
            {
                changedStaticLog.AppendLine(
                    "  * " + GetFullPath(child) +
                    " : StaticFlags " + childFlags + " → " + parentFlags
                );

                GameObjectUtility.SetStaticEditorFlags(childObj, parentFlags);
                staticChanged++;
            }

            ApplyStaticFromParentRecursive(child);
        }
    }

    private string GetFullPath(Transform t)
    {
        if (t == null)
        {
            return string.Empty;
        }

        string path = t.name;
        Transform current = t.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private void AppendFailure(string reason, Transform context)
    {
        failureCount++;

        if (context != null)
        {
            failureLog.AppendLine(
                "  * " + GetFullPath(context) + " : " + reason
            );
        }
        else
        {
            failureLog.AppendLine(
                "  * " + reason
            );
        }
    }

    private void PrintSummary()
    {
        int tilemapTotal = tilemapChanged + tilemapUnchanged;
        int spriteTotal = spriteChanged + spriteUnchanged;
        int staticTotal = staticChanged + staticUnchanged;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("[StageNormalizer] 완료");

        sb.AppendLine(
            "TilemapRenderer 총: " + tilemapTotal +
            "개, 변경: " + tilemapChanged +
            "개, 동일: " + tilemapUnchanged + "개"
        );
        if (tilemapChanged > 0)
        {
            sb.Append(changedTilemapLog.ToString());
        }

        sb.AppendLine(
            "SpriteRenderer 총: " + spriteTotal +
            "개, 변경: " + spriteChanged +
            "개, 동일: " + spriteUnchanged + "개"
        );
        if (spriteChanged > 0)
        {
            sb.Append(changedSpriteLog.ToString());
        }

        sb.AppendLine(
            "Static 플래그 대상 총: " + staticTotal +
            "개, 변경: " + staticChanged +
            "개, 동일: " + staticUnchanged + "개"
        );
        if (staticChanged > 0)
        {
            sb.Append(changedStaticLog.ToString());
        }

        if (failureCount > 0)
        {
            sb.AppendLine("실패/스킵: " + failureCount + "개");
            sb.Append(failureLog.ToString());
        }

        Debug.Log(sb.ToString());
    }
}
#endif
