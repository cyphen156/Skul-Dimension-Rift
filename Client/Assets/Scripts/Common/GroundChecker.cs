using System.Collections.Generic;
using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private BoxCollider2D selfCollider;
    [SerializeField] private Collider2D currentCollision;
    [SerializeField] private bool isInactive;
    public Transform pivotAnchor;

    private void Awake()
    {
        Transform parentTransform = transform.parent;
        BoxCollider2D parent = parentTransform.GetComponent<BoxCollider2D>();
        selfCollider = GetComponent<BoxCollider2D>();

        selfCollider.size = new Vector2(parent.size.x, 0.05f);
        selfCollider.offset = parent.offset;

        groundCheckDistance = 0.1f;
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
        isInactive = false;
    }

    public bool CheckGround()
    {
        //        Vector3 origin = transform.position;
        //        RaycastHit2D hit = Physics2D.Raycast(
        //            origin,
        //            Vector2.down,
        //            groundCheckDistance,
        //            groundMask
        //        );
        //#if UNITY_EDITOR
        //        Debug.DrawRay(origin, Vector2.down * groundCheckDistance, hit ? Color.red : Color.yellow);
        //#endif
        //        if (hit.collider != null)
        //        {
        //            if (currentCollision != hit.collider)
        //            {
        //                currentCollision = hit.collider;
        //            }
        //        }
        //        // hit.collider == null
        //        else
        //        {
        //            if (isInactive == true && currentCollision != null)
        //            {
        //                Physics2D.IgnoreCollision(selfCollider, currentCollision, false);
        //            }

        //            currentCollision = null;
        //            isInactive = false;
        //        }

        //        return hit.collider != null;
        if (isInactive)
        {
            return false;
        }
        return currentCollision != null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isInactive == true)
        {
            if (currentCollision != null)
            {
                Physics2D.IgnoreCollision(selfCollider, currentCollision, false);
            }

            currentCollision = null;
            isInactive = false;
        }

        currentCollision = collision.collider;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (isInactive == true)
        {
            return;
        }

        if (collision.collider == currentCollision)
        {
            currentCollision = null;
        }
    }

    /// <summary>
    /// 특정 충돌을 무시할 때 사용
    /// </summary>
    public void IgnoreCollider(Collider2D target = null)
    {
        if (isInactive == true)
        {
            return;
        }

        Collider2D collider = target;

        if (collider == null)
        {
            collider = currentCollision;
        }

        if (collider == null)
        {
            return;
        }

        Physics2D.IgnoreCollision(selfCollider, collider, true);
        isInactive = true;
    }
}
