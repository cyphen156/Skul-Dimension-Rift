using UnityEngine;

public class CameraBounds : MonoBehaviour
{
    [SerializeField]
    private PolygonCollider2D boundsCollider;

    private void Awake()
    {
        if (boundsCollider == null)
        {
            boundsCollider = GetComponent<PolygonCollider2D>();
        }

        if (CameraManager.instance != null)
        {
            CameraManager.instance.SetBounds(boundsCollider);
        }
    }
}
