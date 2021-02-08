using UnityEngine;

namespace MBaske
{
    public static class Util
    {
        public static bool RandomBool(float probability = 0.5f)
        {
            return Random.value < probability;
        }
    }
}