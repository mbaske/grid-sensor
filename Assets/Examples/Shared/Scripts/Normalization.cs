using UnityEngine;

namespace MBaske.MLUtil
{
    public static class Normalization
    {
        public static float Sigmoid(float val, float scale = 1f)
        {
            val *= scale;
            return val / (1f + Mathf.Abs(val));
        }

        public static Vector3 Sigmoid(Vector3 v3, float scale = 1f)
        {
            v3.x = Sigmoid(v3.x, scale);
            v3.y = Sigmoid(v3.y, scale);
            v3.z = Sigmoid(v3.z, scale);
            return v3;
        }
    }
}