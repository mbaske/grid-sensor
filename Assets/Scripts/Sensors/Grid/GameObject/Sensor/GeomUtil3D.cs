using UnityEngine;

namespace MBaske.Sensors.Grid
{
    public static class GeomUtil3D
    {
        /// <summary>
        /// Returns the polar coordinate for a specified vector.
        /// </summary>
        /// <param name="vector">Cartesian Vector3</param>
        /// <returns>Polar Vector2</returns>
        public static Vector2 GetLonLat(Vector3 vector)
        {
            var up = Vector3.up;
            var proj = Vector3.ProjectOnPlane(vector, up);
            var perp = Vector3.Cross(proj, up);

            var lonLat = new Vector2(
                Vector3.SignedAngle(Vector3.forward, proj, up),
                Vector3.SignedAngle(proj, vector, perp));

            lonLat.x = lonLat.x == 180 ? -180 : lonLat.x;
            lonLat.y = lonLat.y == 90 ? -90 : lonLat.y;

            return lonLat;
        }

        /// <summary>
        /// Calculates a lon/lat rectangle for specified 
        /// view angles and grid cell arc angle.
        /// </summary>
        /// <param name="cellArc">Grid cell arc angle</param>
        /// <param name="lonAngle">FOV's longitude (left & right) angle</param>
        /// <param name="latAngleSouth">FOV's southern latitude (down) angle</param>
        /// <param name="latAngleNorth">FOV's northern latitude (up) angle</param>
        /// <param name="nLon">Number of lon grid cells (output)</param>
        /// <param name="nLat">Number of lat grid cells (output)</param>
        /// <returns>Lon/Lat rectangle</returns>
        public static Rect GetLonLatRect(float cellArc, 
            float lonAngle, float latAngleSouth, float latAngleNorth, 
            out int nLon, out int nLat)
        {
            float lonMin = -lonAngle;
            float lonMax = lonAngle;
            float latMin = -latAngleSouth;
            float latMax = latAngleNorth;

            float lonRange = lonMax - lonMin;
            float latRange = latMax - latMin;

            nLon = Mathf.Max(1, Mathf.CeilToInt(lonRange / cellArc));
            nLat = Mathf.Max(1, Mathf.CeilToInt(latRange / cellArc));

            float lonRangePadded = nLon * cellArc;
            float latRangePadded = nLat * cellArc;

            float lonPadding = (lonRangePadded - lonRange) * 0.5f;
            float latPadding = (latRangePadded - latRange) * 0.5f;

            float lonMinPad = lonMin - lonPadding;
            float lonMinPadClamp = Mathf.Max(-180, lonMinPad);
            float lonMaxPadClamp = Mathf.Min(180, lonMax + lonPadding);

            float latMinPad = latMin - latPadding;
            float latMinPadClamp = Mathf.Max(-90, latMinPad);
            float latMaxPadClamp = Mathf.Min(90, latMax + latPadding);

            return new Rect(
                lonMinPadClamp,
                latMinPadClamp,
                lonMaxPadClamp - lonMinPadClamp,
                latMaxPadClamp - latMinPadClamp);
        }

        /// <summary>
        /// Calculates grid resolution for the specified 
        /// maximum distance and grid cell arc angle.
        /// </summary>
        /// <param name="cellArc">Grid cell arc angle</param>
        /// <param name="maxDistance">Maximum distance</param>
        /// <returns>Length of grid cell</returns>
        public static float GetGridResolution(float cellArc, float maxDistance)
        {
            return Mathf.PI * 2 * maxDistance / (360 / cellArc);
        }

        /// <summary>
        /// Calculates rotations for drawing the Scene GUI wireframe.
        /// </summary>
        /// <param name="cellArc">Grid cell arc angle</param>
        /// <param name="lonLatRect">Lon/Lat rectangle</param>
        /// <param name="shape">Grid shape</param>
        /// <param name="rotations">Rotations [lon, lat]</param>
        public static void CalcGridRotations(float cellArc, Rect lonLatRect, 
            GridBuffer.Shape shape, ref Quaternion[,] rotations)
        {
            int nLon = shape.Width + 1;
            int nLat = shape.Height + 1;

            if (rotations == null || rotations.Length != nLon * nLat)
            {
                rotations = new Quaternion[nLon, nLat];

                float x = lonLatRect.min.x;
                float y = lonLatRect.min.y;

                for (int iLat = 0; iLat < nLat; iLat++)
                {
                    var qLat = Quaternion.AngleAxis(y + iLat * cellArc, Vector3.left);

                    for (int iLon = 0; iLon < nLon; iLon++)
                    {
                        var qLon = Quaternion.AngleAxis(x + iLon * cellArc, Vector3.up);
                        rotations[iLon, iLat] = qLon * qLat;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="Frustum"></see> from a lon/lat rectangle
        /// if none of the polar angles exceed <see cref="Frustum.MaxAngle"/>.
        /// </summary>
        /// <param name="lonLatRect">Lon/Lat rectangle</param>
        /// <param name="near">Near clip plane</param>
        /// <param name="far">Far clip plane</param>
        /// <param name="frustum">Frustum, can be valid or invalid (output)</param>
        /// <returns>True if frustum is valid</returns>
        public static bool HasValidFrustum(Rect lonLatRect, float near, float far, out Frustum frustum)
        {
            // Assuming longitudinal symmetry, min lon = max lon * -1.

            frustum = new Frustum()
            {
                IsValid = lonLatRect.max.x <= Frustum.MaxAngle &&
                          lonLatRect.max.y <= Frustum.MaxAngle &&
                          lonLatRect.min.y >= -Frustum.MaxAngle
            };

            if (frustum.IsValid)
            {
                float f = Mathf.Deg2Rad;
                frustum.Right = Mathf.Tan(lonLatRect.max.x * f);
                frustum.Left = -frustum.Right;

                var qLon = Quaternion.AngleAxis(lonLatRect.max.x, Vector3.up);
                var qLat = Quaternion.AngleAxis(lonLatRect.min.y, Vector3.left);
                Vector3 b = qLon * qLat * Vector3.forward;
                b.x = 0;
                frustum.Bottom = -Mathf.Tan(Vector3.Angle(Vector3.forward, b) * f);

                qLat = Quaternion.AngleAxis(lonLatRect.max.y, Vector3.left);
                Vector3 t = qLon * qLat * Vector3.forward;
                t.x = 0;
                frustum.Top = Mathf.Tan(Vector3.Angle(Vector3.forward, t) * f);

                frustum.Left *= near;
                frustum.Right *= near;
                frustum.Bottom *= near;
                frustum.Top *= near;
                frustum.Near = near;
                frustum.Far = far;
            }

            return frustum.IsValid;
        }

        /// <summary>
        /// Creates a projection matrix for a specified <see cref="Frustum"/>.
        /// </summary>
        /// <param name="frustum">Frustum</param>
        /// <returns>Projection Matrix</returns>
        public static Matrix4x4 GetProjectionMatrix(Frustum frustum)
        {
            
            float x = 2 * frustum.Near / (frustum.Right - frustum.Left);
            float y = 2 * frustum.Near / (frustum.Top - frustum.Bottom);
            float a = (frustum.Right + frustum.Left) / (frustum.Right - frustum.Left);
            float b = (frustum.Top + frustum.Bottom) / (frustum.Top - frustum.Bottom);
            float c = -(frustum.Far + frustum.Near) / (frustum.Far - frustum.Near);
            float d = -(2 * frustum.Far * frustum.Near) / (frustum.Far - frustum.Near);
            float e = -1;

            Matrix4x4 m = new Matrix4x4();
            m[0, 0] = x;
            m[0, 1] = 0;
            m[0, 2] = a;
            m[0, 3] = 0;
            m[1, 0] = 0;
            m[1, 1] = y;
            m[1, 2] = b;
            m[1, 3] = 0;
            m[2, 0] = 0;
            m[2, 1] = 0;
            m[2, 2] = c;
            m[2, 3] = d;
            m[3, 0] = 0;
            m[3, 1] = 0;
            m[3, 2] = e;
            m[3, 3] = 0;

            return m;
        }
    }
}