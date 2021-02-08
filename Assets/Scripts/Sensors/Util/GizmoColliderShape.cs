using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors.Util
{
    /// <summary>
    /// Add this to a collider to draw <see cref="ColliderShape"/> points.
    /// </summary>
    public class GizmoColliderShape : MonoBehaviour
    {
        private HashSet<Vector3> m_Points;
        private float m_Radius;

        private void Awake()
        {
            m_Points = new HashSet<Vector3>();
        }

        public void AddScanPoints(HashSet<Vector3> localPoints, float resolution)
        {
            m_Points.UnionWith(localPoints);
            m_Radius = resolution * 0.25f;
        }

        private void OnDrawGizmos()
        {
            if (m_Points != null)
            {
                Gizmos.color = Color.red;
                foreach (var p in m_Points)
                {
                    Gizmos.DrawSphere(transform.TransformPoint(p), m_Radius);
                }
            }
        }
    }
}