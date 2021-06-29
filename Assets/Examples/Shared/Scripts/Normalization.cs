using UnityEngine;

namespace MBaske.MLUtil
{
    /// <summary>
    /// Static utility class for normalizing observations.
    /// </summary>
    public static class Normalization
    {
        /// <summary>
        /// Sigmoid: value * scale / (1 + abs(value * scale))
        /// </summary>
        /// <param name="value">Input value</param>
        /// <param name="scale">Input scale</param>
        /// <returns>Normalized value</returns>
        public static float Sigmoid(float value, float scale = 1f)
        {
            value *= scale;
            return value / (1f + Mathf.Abs(value));
        }

        /// <summary>
        /// Sigmoid: value * scale / (1 + abs(value * scale))
        /// </summary>
        /// <param name="vector">Input vector</param>
        /// <param name="scale">Input scale</param>
        /// <returns>Normalized vector</returns>
        public static Vector3 Sigmoid(Vector3 vector, float scale = 1f)
        {
            vector.x = Sigmoid(vector.x, scale);
            vector.y = Sigmoid(vector.y, scale);
            vector.z = Sigmoid(vector.z, scale);
            return vector;
        }
    }
}