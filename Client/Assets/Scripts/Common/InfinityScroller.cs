using Assets.Scripts.Interface;
using UnityEngine;

public class InfinityScroller : MonoBehaviour, IMoveable
{
    //[SerializeField] private float width;
    //[SerializeField] private float height;

    //private Vector3 lastPosition;

    //private void Start()
    //{
    //    if (followTarget != null)
    //    {
    //        lastPosition = followTarget.position;
    //    }
    //}

    public void Move(Vector3 delta)
    {
        //    transform.Translate(delta);

        //    if (followTarget == null)
        //    {
        //        return;
        //    }

        //    Vector3 followPos = followTarget.position;
        //    float diff = followPos.x - transform.position.x;

        //    // 왼쪽으로 너무 멀리 벗어났다면 오른쪽으로 이동
        //    if (diff > threshold)
        //    {
        //        transform.position += Vector3.right * width;
        //    }
        //    // 오른쪽으로 너무 멀리 벗어났다면 왼쪽으로 이동
        //    else if (diff < -threshold)
        //    {
        //        transform.position -= Vector3.right * width;
        //    }

        //    lastPosition = followPos;
    }

    public void Move(Vector2 delta)
    {
    //    Move((Vector3)delta);
    }

    public void Jump(float jumpForce)
    {
    //    // 패럴렉스 배경은 점프 개념 없음
    }
}
