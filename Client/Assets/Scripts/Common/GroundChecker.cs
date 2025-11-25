using System;
using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private LayerMask groundMask;

    private void Awake()
    {
        groundCheckDistance = 0.1f;
        groundMask = LayerMask.GetMask("Ground");
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
        Debug.DrawRay(origin, Vector2.down * groundCheckDistance, hit ? Color.green : Color.red);
#endif
        return hit.collider != null;
    }
}
