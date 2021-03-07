using UnityEngine;
using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    [System.Serializable]
    public class GameObjectShape3D : GameObjectShape
    {
        [Header("LOD")]
        [SerializeField, Tooltip("Edit to show different detail levels.\n0: no reduction"
            + " / highest detail. Values are set at runtime depending on sensor distance.")]
        protected int m_PointReduction;
        [SerializeField, ReadOnly, Tooltip("Maximum available point reduction.")]
        protected int m_MaxReduction;

        public override void OnReset()
        {
            base.OnReset();
            m_PointReduction = 0;
            m_MaxReduction = 0;
        }

        public override void OnValidate()
        {
            base.OnValidate();
            m_PointReduction = Mathf.Clamp(m_PointReduction, 0, m_MaxReduction);
            if (m_IsScanned)
            {
                SetLODPoints();
            }
        }

        public override void OnScanResult(List<Vector3>[] result)
        {
            base.OnScanResult(result);
            m_MaxReduction = m_ScanResult.Length - 1;
            m_PointReduction = 0;
            SetLODPoints();
        }

        public override IList<Vector3> LocalToWorld(Transform transform, float normDistance)
        {
            // Runtime update, overrides inspector value.
            m_PointReduction = Mathf.RoundToInt(normDistance * m_MaxReduction);
            SetLODPoints();
            return base.LocalToWorld(transform);
        }

        private void SetLODPoints()
        {
            m_LocalPoints = m_ScanResult[m_PointReduction].LocalPoints;
            m_PointCount = m_LocalPoints.Count; 
        }
    }
}