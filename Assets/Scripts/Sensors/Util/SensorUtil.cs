
using UnityEngine;

namespace MBaske.Sensors.Util
{
    public static class Normalization
    {
        public static float InvSigmoid(float norm, float weight)
        {
            return 1 - norm / (weight + Mathf.Abs(norm)) * (weight + 1);
        }
    }
}