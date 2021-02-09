
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

    public static class Geometry
    {
        public static Vector2 GetLonLat(Transform referenceFrame, Vector3 vector)
        {
            var up = referenceFrame.up;
            var proj = Vector3.ProjectOnPlane(vector, up);
            var perp = Vector3.Cross(proj, up);

            var lonLat = new Vector2(
                Vector3.SignedAngle(referenceFrame.forward, proj, up),
                Vector3.SignedAngle(proj, vector, perp));

            lonLat.x = lonLat.x == 180 ? -180 : lonLat.x;
            lonLat.y = lonLat.y == 180 ? -180 : lonLat.y;

            return lonLat;
        }
    }
}