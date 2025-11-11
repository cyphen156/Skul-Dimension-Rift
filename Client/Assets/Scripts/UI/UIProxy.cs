using Assets.Scripts.Interface;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIProxy : InteractiveUIBehaviour
{
    [SerializeField] public InteractiveUIBehaviour bound;
    [SerializeField] private RectTransform boundTransform; 
    [SerializeField] public RectTransform localTransform;
    [SerializeField] private RectTransform rootTransform;

    private new void Awake()
    {
        if (localTransform == null)
        {
            localTransform = GetComponent<RectTransform>();
        }

        rootTransform = localTransform != null ? localTransform.parent as RectTransform : null;
    }

    private new void OnDisable()
    {
        bound = null;
        boundTransform = null;
    }

    public void Bind(InteractiveUIBehaviour bindTargetUI)
    {
        bound = bindTargetUI;
        boundTransform = bound != null ? bound.GetComponent<RectTransform>() : null;

        if (localTransform == null)
        {
            localTransform = GetComponent<RectTransform>();
        }

        rootTransform = localTransform != null ? localTransform.parent as RectTransform : null;

        SyncProxyArea();
    }

    /// <summary>
    /// 해상도 변경, 옵션 적용(해상도/스케일), 레이아웃 리빌드 등 변화가 있을 때 외부 호출
    /// </summary>
    public override void Refresh(string key)
    {
        SyncProxyArea();

        bound.Refresh(key);
    }

    private void SyncProxyArea()
    {
        if (boundTransform == null || localTransform == null || rootTransform == null)
        {
            return;
        }

        Vector3[] worldCorners = new Vector3[4];
        boundTransform.GetWorldCorners(worldCorners);

        Vector2 localMin;
        Vector2 localMax;

        Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(null, worldCorners[0]);
        Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(null, worldCorners[2]);

        bool hasMin = RectTransformUtility.ScreenPointToLocalPointInRectangle(rootTransform, screenMin, null, out localMin);
        bool hasMax = RectTransformUtility.ScreenPointToLocalPointInRectangle(rootTransform, screenMax, null, out localMax);

        if (!hasMin || !hasMax)
        {
            return;
        }

        Vector2 size = localMax - localMin;
        localTransform.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        Vector2 center = (localMin + localMax) * 0.5f;
        localTransform.anchoredPosition = center;
    }

    public override void Execute(InputAction.CallbackContext ctx)
    {
        if (bound == null)
        {
            return;
        }

        string actionName = ctx.action.name;
        // 포인트 점검
        if (actionName == "Point" && ctx.performed)
        {
            HandlePoint(ctx);
        }

        if (actionName == "Click")
        {
            // 클릭 시작 - 영역 밖이면 무시
            if (ctx.started)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(localTransform, lastPoint))
                {
                    return;
                }
            }

            // 클릭 종료 - 영역 밖이면 취소
            if (ctx.canceled)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(localTransform, lastPoint))
                {
                    return;
                }
            }
        }

        bound.Execute(ctx);
    }
}
