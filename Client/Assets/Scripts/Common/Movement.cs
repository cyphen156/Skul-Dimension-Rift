using Assets.Scripts.Interface;
using UnityEngine;

namespace Assets.Scripts.Common
{
    public class Movement : MonoBehaviour, IMoveable
    {
        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public void Move(Vector2 direction)
        {
            rb.linearVelocityX = direction.x;
        }

        public void Move(Vector3 direction)
        {
            rb.linearVelocityX = direction.x;
        }

        public void Jump(Vector2 jumpDirection, float jumpForce)
        {
            rb.AddForce(jumpDirection.normalized * jumpForce, ForceMode2D.Impulse);
        }
    }
}
