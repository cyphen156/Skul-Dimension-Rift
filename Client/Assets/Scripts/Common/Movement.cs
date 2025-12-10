using Assets.Scripts.Interface;
using UnityEngine;

namespace Assets.Scripts.Common
{
    public class Movement : MonoBehaviour, IMoveable
    {
        private Rigidbody2D rb;
        [SerializeField] private float maxGravityAcc;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            maxGravityAcc = Physics2D.gravity.y;
        }
        private void FixedUpdate()
        {
            if (rb.linearVelocityY < maxGravityAcc)
            {
                rb.linearVelocityY = maxGravityAcc;
            }
        }

        public void Move(Vector2 direction)
        {
            rb.linearVelocityX = direction.x;
        }

        public void Move(Vector3 direction)
        {
            rb.linearVelocityX = direction.x;
        }

        public void Jump(float jumpForce)
        {
            rb.linearVelocityY = jumpForce;
            //rb.AddForce(jumpDirection.normalized * jumpForce, ForceMode2D.Impulse);
        }
    }
}
