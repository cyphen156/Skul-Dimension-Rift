using Assets.Scripts.Interface;
using UnityEngine;

namespace Assets.Scripts.Common
{
    public class Movement : MonoBehaviour, IMoveable
    {
        public void Move(Vector2 direction)
        {
            transform.Translate(direction * Time.deltaTime);
        }

        public void Jump(float power)
        {
            transform.Translate(Vector2.up * power);
        }
    }
}
