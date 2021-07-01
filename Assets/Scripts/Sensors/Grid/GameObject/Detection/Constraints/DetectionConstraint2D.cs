using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Constraint for box / 2D detection.
    /// </summary>
    public class DetectionConstraint2D : DetectionConstraint
    {
        /// <summary>
        /// Detection bounds.
        /// </summary>
        public Bounds Bounds
        {
            set
            {
                m_Bounds = value;
                m_BoundRect = new Rect(value.min.x, value.min.z, value.size.x, value.size.z);
            }
            get { return m_Bounds; }
        }
        private Bounds m_Bounds;
        private Rect m_BoundRect;

        /// <inheritdoc/>
        public override bool ContainsPoint(Vector3 localPoint, out Vector3 normPoint)
        {
            if (m_Bounds.Contains(localPoint))
            {
                normPoint = Rect.PointToNormalized(m_BoundRect, new Vector2(localPoint.x, localPoint.z));
                return true;
            }

            normPoint = default;
            return false;
        }
    }
}
