using UnityEngine;

namespace MBaske.Sensors.Grid
{
    public class GameObjectDetector3D : GameObjectDetector
    {
        public Constraint3D Constraint
        {
            set { m_Constraint = value; }
        }
        private Constraint3D m_Constraint;

        public override DetectionResult Update()
        {
            Result.Clear();

            ParseColliders(
                Physics.OverlapSphereNonAlloc(m_Transform.position,
                    m_Constraint.MaxRadius, m_Buffer, m_LayerMask),
                m_Constraint, m_Transform.worldToLocalMatrix);

            return Result;
        }
    }

    public class Constraint3D : Constraint
    {
        public float MinRadius
        {
            set
            {
                m_MinRadius = value;
                m_MinRadiusSqr = value * value;
                m_Range = m_MaxRadius - m_MinRadius;
            }
        }
        private float m_MinRadius;
        private float m_MinRadiusSqr;

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

        public Rect LonLatRect
        {
            set { m_LonLatRect = value; }
        }
        private Rect m_LonLatRect;

        public override bool ContainsPoint(Vector3 localPoint, out Vector3 normalized)
        {
            float radiusSqr = localPoint.sqrMagnitude;

            if (radiusSqr >= m_MinRadiusSqr && radiusSqr <= m_MaxRadiusSqr)
            {
                Vector2 lonLat = GetLonLat(localPoint);
                if (m_LonLatRect.Contains(lonLat))
                {
                    normalized = Rect.PointToNormalized(m_LonLatRect, lonLat);
                    normalized.z = Mathf.Clamp01((Mathf.Sqrt(radiusSqr) - m_MinRadius) / m_Range); // 0 (near) to 1 (far)
                    return true;
                }
            }

            normalized = default;
            return false;
        }

        public override float NormalizeDistance(Vector3 localPoint)
        {
            return Mathf.Clamp01((localPoint.magnitude - m_MinRadius) / m_Range); // 0 (near) to 1 (far)
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