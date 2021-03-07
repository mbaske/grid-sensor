using System;
using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    [Serializable]
    public class ScanResultWrapper
    {
        public List<Vector3> LocalPoints;
    }

    [Serializable]
    public abstract class GameObjectShape
    {
        [SerializeField, HideInInspector]
        protected ScanResultWrapper[] m_ScanResult;
        [SerializeField, HideInInspector]
        protected List<Vector3> m_LocalPoints;
        [SerializeField, HideInInspector]
        protected List<Vector3> m_WorldPoints;

        [Help("Enable Gizmos to scan colliders in scene view and draw the detectable points.")]

        [SerializeField, Tooltip("Whether to scan the colliders at runtime."
            + "\nEnable if runtime colliders differ from prefab.")]
        protected bool m_ScanAtRuntime;
        public bool ScanAtRuntime
        {
            get { return m_ScanAtRuntime; }
        }
        
        [SerializeField, Tooltip("Distance between scan points.")]
        [Min(0.1f)] protected float m_ScanResolution = 0.25f;
        [SerializeField, HideInInspector]
        private float m_ScanResolutionTmp = 0.25f;
        public float ScanResolution
        {
            get { return m_ScanResolution; }
        }

        [SerializeField, ReadOnly, Tooltip("Resulting number of detectable points."
            + "\nTry a different scan resolution if value is 0.")]
        protected int m_PointCount;

        [SerializeField, ReadOnly, Tooltip("Whether the colliders have been scanned.")]
        protected bool m_IsScanned;
        public bool IsScanned
        {
            get { return m_IsScanned; }
            set { m_IsScanned = value; }
        }
        

        // Gizmos.

        [Space, SerializeField, Tooltip("Whether to continuously rescan the colliders."
            + "\nUsed for testing purposes. Disabled at runtime.")]
        protected bool m_GizmoRescan;
        [SerializeField, Tooltip("Edit to show different grid resolutions."
            + "\nThis is meant for testing purposes and doesn't reflect the"
            + " grid settings applied to the sensor.\nGizmo grid cells are drawn"
            + " in world space.\n0: Grid draw off. Disabled at runtime.")]
        [Min(0)] protected float m_GizmoDrawGrid = 0;

        public bool GizmoForceScan() => m_GizmoRescan || !m_IsScanned;

        public bool GizmoHasPoints(out IList<Vector3> points)
        {
            points = m_LocalPoints;
            return m_LocalPoints != null;
        }

        public bool GizmoDrawGrid(out float resolution)
        {
            resolution = m_GizmoDrawGrid;
            return m_GizmoDrawGrid >= 0.1f;
        }



        public virtual void OnReset()
        {
            if (m_IsScanned)
            {
                m_IsScanned = false;
                m_ScanResult = null;
                m_LocalPoints.Clear();
                m_WorldPoints.Clear();
            }

            m_ScanResolution = 0.25f;
            m_ScanResolutionTmp = 0.25f;
            m_GizmoDrawGrid = 0;
            m_PointCount = 0;
            m_ScanAtRuntime = false;
            m_GizmoRescan = false;
        }

        public virtual void OnValidate()
        {
            if (m_ScanResolutionTmp != ScanResolution)
            {
                m_ScanResolutionTmp = ScanResolution;
                m_IsScanned = false;
            }
        }

        // Runtime or scene edit.
        public virtual void OnScanResult(List<Vector3>[] result)
        {
            m_ScanResult = new ScanResultWrapper[result.Length];
            for (int i = 0; i < result.Length; i++)
            {
                m_ScanResult[i] = new ScanResultWrapper() { LocalPoints = result[i] };
            }
            m_PointCount = result[0].Count;
            m_LocalPoints = new List<Vector3>(result[0]);
            m_WorldPoints = new List<Vector3>(m_PointCount);
            m_IsScanned = true;
        }

        public virtual IList<Vector3> LocalToWorld(Transform transform, float normDistance = 0)
        {
            Matrix4x4 matrix = transform.localToWorldMatrix;
            m_WorldPoints.Clear();
            for (int i = 0; i < m_PointCount; i++)
            {
                m_WorldPoints.Add(matrix.MultiplyPoint3x4(m_LocalPoints[i]));
            }
            return m_WorldPoints;
        }
    }
}