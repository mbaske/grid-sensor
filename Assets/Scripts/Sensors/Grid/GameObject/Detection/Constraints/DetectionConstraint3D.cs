using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Constraint for spherical / 3D detection.
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


        /// <inheritdoc/>
        public override bool ContainsPoint(Vector3 localPoint, out Vector3 normPoint)
        {
            float radiusSqr = localPoint.sqrMagnitude;

            if (radiusSqr >= m_MinRadiusSqr && radiusSqr <= m_MaxRadiusSqr)
            {
                Vector2 lonLat = GeomUtil3D.GetLonLat(localPoint);
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
    }
}