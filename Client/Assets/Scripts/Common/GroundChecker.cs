using System.Collections.Generic;
using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private BoxCollider2D selfCollider;
    public Transform pivotAnchor;

    private void Awake()
    {
        Transform parentTransform = transform.parent;
        BoxCollider2D parent = parentTransform.GetComponent<BoxCollider2D>();
        selfCollider = GetComponent<BoxCollider2D>();

        selfCollider.size = new Vector2(parent.size.x, 0.05f);
        selfCollider.offset = parent.offset;

        groundCheckDistance = 0.05f;
        groundMask = LayerMask.GetMask("Ground");

        if (pivotAnchor == null)
        {
            List<Transform> transforms = ComponentRegistrar.RegisterComponentsInChildren<Transform>(transform.parent, includeInactive: true, continuous:true);

            foreach (Transform t in transforms)
            {
                if (t.gameObject.name == "PivotAnchor")
                {
                    pivotAnchor = t;
                    break;
                }
            }

            // 뒤져봤는데 없으면 자기 자신으로 설정
            pivotAnchor = pivotAnchor ?? transform;
        }

        transform.position = pivotAnchor.position;
    }
    public bool CheckGround()
    {
        Vector3 origin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            groundCheckDistance,
            groundMask
        );
#if UNITY_EDITOR
        Debug.DrawRay(origin, Vector2.down * groundCheckDistance, hit ? Color.red : Color.yellow);
#endif
        return hit.collider != null;
    }

    public void IgnoreGround()
    {
    }
}
