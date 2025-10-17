using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// root의 자식들 중 T 타입의 컴포넌트를 찾아 List로 반환합니다.
/// includeInactive가 true이면 비활성화된 오브젝트도 탐색합니다.
/// 자식의 자식도 탐색합니다.
/// </summary>
public static class ComponentRegistrar
{
    public static List<T> RegisterComponentsInChildren<T>(
        Transform root, 
        int maxCount = 0, int minDepth = 0, int maxDepth = 0, 
        bool includeInactive = false, 
        bool continuous = false /* use this Option as find all components in Children default = false*/)
        where T : Object
    {
        List<T> components = new List<T>();
        RegisterComponentsRecursive(root, components, 0, minDepth, maxDepth, maxCount, includeInactive, continuous);
        return components;
    }

    private static void RegisterComponentsRecursive<T>(
        Transform current, List<T> components, int currentDepth, int minDepth, int maxDepth, int maxCount, bool includeInactive, bool continuous)
        where T : Object
    {
        if (maxCount > 0 && components.Count >= maxCount)
            return;
        if (currentDepth >= minDepth && (maxDepth == 0 || currentDepth <= maxDepth))
        {
            T component = current.GetComponent<T>();
            if (component != null)
            {
                components.Add(component);
                if (!continuous)
                {
                    return;
                }
            }
        }
        foreach (Transform child in current)
        {
            if (includeInactive || child.gameObject.activeInHierarchy)
            {
                RegisterComponentsRecursive(child, components, currentDepth + 1, minDepth, maxDepth, maxCount, includeInactive, continuous);
            }
        }
    }
}
