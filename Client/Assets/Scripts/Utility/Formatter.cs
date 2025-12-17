using UnityEngine;

namespace Assets.Scripts.Utility
{
    public static class Formatter
    {
        public static string Vec2Resolution(Vector2 vector)
        {
            string str = vector.x.ToString() + " X "+ vector.y.ToString();
            return str;
        }

        public static string ToDebugString(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is Vector2 vec2)
            {
                return Vec2Resolution(vec2);
            }

            if (value is uint u32)
            {
                return "0x" + u32.ToString("X8");
            }

            return value.ToString();
        }
    }
}
