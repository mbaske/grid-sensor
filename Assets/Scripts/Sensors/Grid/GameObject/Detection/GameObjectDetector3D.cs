using UnityEngine;
using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Detects gameobjects located in 3D space.
    /// </summary>
    public class GameObjectDetector3D : GameObjectDetector
    {
        /// <summary>
        /// Constraint for 3D detection.
        /// </summary>
        public DetectionConstraint3D Constraint
        {
            set { m_Constraint = value; ValidateCullingEnabled(); }
        }
        private DetectionConstraint3D m_Constraint;

        private HashSet<Collider> m_ColliderBufferSet;
        private bool m_CullingEnabled;
        private Camera m_Camera;
        private Plane[] m_Planes;

        private void ValidateCullingEnabled()
        {
            m_CullingEnabled = m_Constraint.FrustumSides.w > 0; // flag

            if (m_CullingEnabled)
            {
                // Create a helper camera for FOV angles < 87, 
                // so we can cull the found colliders before the
                // detector has to parse all their shape points.

                var nested = m_Transform.Find("HelperCam");
                if (nested == null)
                {
                    nested = new GameObject("HelperCam").transform;
                    // Cam frustum does update even with disabled
                    // camera component and inactive gameobject.
                    nested.gameObject.SetActive(false);
                    nested.parent = m_Transform;
                    nested.localPosition = Vector3.zero;

                    m_Camera = nested.gameObject.AddComponent<Camera>();
                    m_Camera.cullingMask = m_LayerMask;
                    m_Camera.enabled = false;
                }
                else
                {
                    m_Camera = nested.GetComponent<Camera>();
                }

                float near = Mathf.Max(m_Constraint.MinRadius, 0.01f);
                float far = m_Constraint.MaxRadius;

                m_Camera.nearClipPlane = near;
                m_Camera.farClipPlane = far;

                Vector3 fs = m_Constraint.FrustumSides;
                m_Camera.projectionMatrix = GetProjectionMatrix(
                    -fs.x * near,
                    fs.x * near,
                    -fs.y * near,
                    fs.z * near,
                    near, far);

                m_ColliderBufferSet = new HashSet<Collider>();
                m_Planes = new Plane[6];
            }
        }

        /// <inheritdoc/>
        public override void OnSensorUpdate()
        {
            base.OnSensorUpdate();

            int numFound = 0;
            do
            {
                ValidateColliderBufferSize(numFound);
                numFound = Physics.OverlapSphereNonAlloc(
                    m_Transform.position,
                    m_Constraint.MaxRadius, 
                    m_ColliderBuffer, 
                    m_LayerMask);
            }
            while (numFound == m_ColliderBufferSize);


            if (m_CullingEnabled)
            {
                m_ColliderBufferSet.Clear();
                GeometryUtility.CalculateFrustumPlanes(m_Camera, m_Planes);

                for (int i = 0; i < numFound; i++)
                {
                    var cld = m_ColliderBuffer[i];
                    if (GeometryUtility.TestPlanesAABB(m_Planes, cld.bounds))
                    {
                        m_ColliderBufferSet.Add(cld);
                    }
                }
                m_ColliderBufferSet.CopyTo(m_ColliderBuffer);
                //Debug.Log($"Culled: {numFound} > {m_ColliderBufferSet.Count}");
                numFound = m_ColliderBufferSet.Count;
            }

            ParseColliders(numFound, m_Constraint, m_Transform.worldToLocalMatrix);
        }

        private static Matrix4x4 GetProjectionMatrix(
            float left, float right, float bottom, float top, float near, float far)
        {
            float x = 2 * near / (right - left);
            float y = 2 * near / (top - bottom);
            float a = (right + left) / (right - left);
            float b = (top + bottom) / (top - bottom);
            float c = -(far + near) / (far - near);
            float d = -(2 * far * near) / (far - near);
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

    /// <summary>
    /// Constraint for 3D detection.
    /// </summary>
    public class DetectionConstraint3D : DetectionConstraint
    {
        /// <summary>
        /// The minimum detection distance (near clipping).
        /// </summary>
        public float MinRadius
        {
            set
            {
                m_MinRadius = value;
                m_MinRadiusSqr = value * value;
                m_Range = m_MaxRadius - m_MinRadius;
            }
            get { return m_MinRadius; }
        }
        private float m_MinRadius;
        private float m_MinRadiusSqr;

        /// <summary>
        /// The maximum detection distance (far clipping).
        /// </summary>
        public float MaxRadius
        {
            set
            {
                m_MaxRadius = value;
                m_MaxRadiusSqr = value * value;
                m_Range = m_MaxRadius - m_MinRadius;
            }
            get { return m_MaxRadius; }
        }
        private float m_MaxRadius;
        private float m_MaxRadiusSqr;
        private float m_Range;

        /// <summary>
        /// Longitude/Latitude rectangle.
        /// </summary>
        public Rect LonLatRect;

        /// <summary>
        /// Unscaled frustum sides, positive values.
        /// x - left/right (lon)
        /// y - bottom (lat)
        /// z - top (lat)
        /// w - enabled flag
        /// </summary>
        public Vector4 FrustumSides;

        /// <inheritdoc/>
        public override bool ContainsPoint(Vector3 localPoint, out Vector3 normPoint)
        {
            float radiusSqr = localPoint.sqrMagnitude;

            if (radiusSqr >= m_MinRadiusSqr && radiusSqr <= m_MaxRadiusSqr)
            {
                Vector2 lonLat = GetLonLat(localPoint);
                if (LonLatRect.Contains(lonLat))
                {
                    normPoint = Rect.PointToNormalized(LonLatRect, lonLat);
                    // 0 (near) to 1 (far)
                    normPoint.z = Mathf.Clamp01((Mathf.Sqrt(radiusSqr) - m_MinRadius) / m_Range); 
                    return true;
                }
            }

            normPoint = default;
            return false;
        }

        /// <inheritdoc/>
        public override float GetNormalizedDistance(Vector3 localPoint)
        {
            // 0 (near) to 1 (far)
            return Mathf.Clamp01((localPoint.magnitude - m_MinRadius) / m_Range);
        }

        private static Vector2 GetLonLat(Vector3 v)
        {
            var up = Vector3.up;
            var proj = Vector3.ProjectOnPlane(v, up);
            var perp = Vector3.Cross(proj, up);

            var lonLat = new Vector2(
                Vector3.SignedAngle(Vector3.forward, proj, up),
                Vector3.SignedAngle(proj, v, perp));

            lonLat.x = lonLat.x == 180 ? -180 : lonLat.x;
            lonLat.y = lonLat.y == 180 ? -180 : lonLat.y;

            return lonLat;
        }
    }
}