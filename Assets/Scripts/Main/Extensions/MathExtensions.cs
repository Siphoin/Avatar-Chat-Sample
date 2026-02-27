using UnityEngine;

namespace AvatarChat.Main.Extensions
{
    public class MathExtensions
    {
        public static float ClampNegative (float value, float min = 0)
        {
            return value < min ? min : value;
        }

        public static float ClampNegative(float value, float max, float min = 0)
        {
            return Mathf.Clamp(value, min, max);
        }
    }
}
