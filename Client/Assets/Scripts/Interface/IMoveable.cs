using UnityEngine;

namespace Assets.Scripts.Interface
{
    public interface IMoveable
    {
        void Move(Vector2 direction);
        void Move(Vector3 direction);
        void Jump(Vector2 jumpDirection, float jumpForce);
    }
}
