using UnityEngine;

namespace Assets.Scripts.Common
{
    public static class Formatter
    {
        public static string Vec2Resolution(Vector2 vector)
        {
            string str = vector.x.ToString() + " X "+ vector.y.ToString();
            return str;
        }
    }
}
