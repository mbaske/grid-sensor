using System.Collections.Generic;
using System;
using UnityEngine;
using MBaske.Sensors.Util;

namespace MBaske.Sensors
{
    /// <summary>
    /// ColliderShape generates and stores a set 
    /// of points located within a detected collider.
    /// </summary>
    public class ColliderShape
    {
        public int NumPoints { get; private set;  }

        private readonly int m_Mask;
        private readonly Collider m_Collider;
        private readonly Transform m_Transform;
        private readonly Collider[] m_Buffer;
        private HashSet<Vector3> m_LocalPoints;
        private HashSet<Vector3> m_WorldPoints;
        private Quaternion m_Rotation;
        private Vector3 m_Position;

        public ColliderShape(Collider collider)
        {
            // TODO how to handle compound colliders?
            m_Buffer = new Collider[4];
            m_Collider = collider;
            m_Transform = collider.transform;
            m_Mask = 1 << collider.gameObject.layer;
            m_Position = m_Transform.position;
            m_Rotation = m_Transform.rotation;
        }

        /// <summary>
        /// <returns>The stored points in world reference frame.</returns>
        /// </summary>
        public HashSet<Vector3> GetWorldPoints()
        {
            if (m_Transform.position != m_Position || m_Transform.rotation != m_Rotation)
            {
                m_Position = m_Transform.position;
                m_Rotation = m_Transform.rotation;
                TransformPoints();
            }
            return m_WorldPoints;
        }

        private void TransformPoints()
        {
            m_WorldPoints.Clear();
            foreach (var point in m_LocalPoints)
            {
                m_WorldPoints.Add(m_Transform.TransformPoint(point));
            }
        }

        /// <summary>
        /// Scans a collider's shape by performing a flood fill.
        /// Reduces the detected points by hollowing out the resulting shape.
        /// <param name="resolution">Distance between points.</param>
        /// <param name="extent">Maximum axis-aligned point distance from collider center.</param>
        /// </summary>
        public void Scan(float resolution, float extent)
        {
            // Flood fill.
            extent *= extent;
            Vector3Int[] grid = s_GridPoints[0];
            Vector3 center = m_Collider.bounds.center;

            var map = new Dictionary<Vector3Int, Vector3>();
            var visited = new HashSet<Vector3Int> { Vector3Int.zero };
            var pending = new Stack<Vector3Int>();
            pending.Push(Vector3Int.zero);
            
            while (pending.Count > 0)
            {
                Vector3Int pGridK = pending.Pop();
                Vector3 pWorld = center + m_Rotation * pGridK * resolution;

                if ((pWorld - center).sqrMagnitude <= extent && IsInsideCollider(pWorld))
                {
                    map.Add(pGridK, m_Transform.InverseTransformPoint(pWorld));

                    foreach (Vector3Int pGrid in grid)
                    {
                        Vector3Int neighbour = pGridK + pGrid;

                        if (visited.Add(neighbour))
                        {
                            pending.Push(neighbour);
                        }
                    }
                }
            }

            // Hollow out grid. 
            // Use s_GridPoints[1] for removing less points.
            grid = s_GridPoints[0];
            m_LocalPoints = new HashSet<Vector3>();
       
            foreach (var kvp in map)
            {
                Vector3Int pGridK = kvp.Key;
                bool remove = true;

                foreach (Vector3Int pGrid in grid)
                {
                    remove = remove && map.ContainsKey(pGridK + pGrid);
                }
                if (!remove)
                {
                    m_LocalPoints.Add(kvp.Value);
                }
            }

            m_WorldPoints = new HashSet<Vector3>();
            TransformPoints();
            NumPoints = m_WorldPoints.Count;

            if (m_Collider.TryGetComponent(out GizmoColliderShape comp))
            {
                comp.AddScanPoints(m_LocalPoints, resolution);
            }
        }

        private bool IsInsideCollider(Vector3 p)
        {
            int n = Physics.OverlapSphereNonAlloc(p, 0, m_Buffer, m_Mask);
            return n > 0 && Array.IndexOf(m_Buffer, m_Collider) != -1;
        }

        private static readonly Vector3Int[][] s_GridPoints = new Vector3Int[][]
        {
            new Vector3Int[]
            {
                Vector3Int.left,
                Vector3Int.right,
                Vector3Int.down,
                Vector3Int.up,
                Vector3Int.back,
                Vector3Int.forward
            },
            new Vector3Int[]
            {
                Vector3Int.left,
                Vector3Int.right,
                Vector3Int.down,
                Vector3Int.up,
                Vector3Int.back,
                Vector3Int.forward,

                Vector3Int.left + Vector3Int.down,
                Vector3Int.left + Vector3Int.up,
                Vector3Int.right + Vector3Int.down,
                Vector3Int.right + Vector3Int.up,
                Vector3Int.back + Vector3Int.down,
                Vector3Int.back + Vector3Int.up,
                Vector3Int.forward + Vector3Int.down,
                Vector3Int.forward + Vector3Int.up,

                Vector3Int.left + Vector3Int.forward + Vector3Int.down,
                Vector3Int.left + Vector3Int.forward + Vector3Int.up,
                Vector3Int.right + Vector3Int.forward + Vector3Int.down,
                Vector3Int.right + Vector3Int.forward + Vector3Int.up,
                Vector3Int.left + Vector3Int.back + Vector3Int.down,
                Vector3Int.left + Vector3Int.back + Vector3Int.up,
                Vector3Int.right + Vector3Int.back + Vector3Int.down,
                Vector3Int.right + Vector3Int.back + Vector3Int.up
            }
        };
    }
}





