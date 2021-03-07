using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    public abstract class GameObjectDetector : Detector
    {
        public int BufferSize
        {
            set 
            {
                m_BufferSize = value;
                m_Buffer = new Collider[value]; 
            }
        }
        protected Collider[] m_Buffer;
        protected int m_BufferSize;

        public bool ClearCacheOnReset
        {
            set { m_ClearCacheOnReset = value; }
        }
        protected bool m_ClearCacheOnReset;

        public Transform SensorTransform
        {
            set { m_Transform = value; }
        }
        protected Transform m_Transform;

        // Optional.
        public DetectableGameObject SensorOwner
        {
            set { m_Owner = value; }
        }
        protected DetectableGameObject m_Owner;

        public GameObjectSettingsByTag Settings
        {
            set 
            {
                m_Settings = value;
                m_LayerMask = value.LayerMask; 
                Result = new DetectionResult(value.DetectableTags, m_BufferSize);
            }
        }
        protected GameObjectSettingsByTag m_Settings;
        protected int m_LayerMask;
        // Capacity?
        private readonly List<Vector3> m_TmpNormPoints = new List<Vector3>(1024);
        private readonly List<Vector3> m_TmpWorldPoints = new List<Vector3>(1024);

        protected void ParseColliders(int n, Constraint constraint, Matrix4x4 worldToLocalMatrix)
        {
            Vector3 sensorPos = m_Transform.position;

            for (int i = 0; i < n; i++)
            {
                if (m_Settings.IsDetectableTag(m_Buffer[i].tag, out ColliderDetectionType type))
                {
                    var detectable = DetectableGameObject.GetCached(m_Buffer[i]);
                    // Need to filter out compound collider duplicate results,
                    // as each collider is a key in DetectableGameObject's cache.
                    // NOTE
                    // We also skip the sensor owner if there's a DetectableGameObject 
                    // parent to the sensor.
                    // Although the owner could be excluded by setting the minimum detection
                    // distance large enough for 3D, it's better to avoid checking the
                    // owner's points at every step in the first place.
                    if (detectable != m_Owner && !Result.Contains(detectable))
                    {
                        m_TmpNormPoints.Clear();
                        m_TmpWorldPoints.Clear();

                        switch (type)
                        {
                            case ColliderDetectionType.Position:
                                m_TmpWorldPoints.Add(detectable.GetPosition());
                                break;

                            case ColliderDetectionType.ClosestPoint:
                                m_TmpWorldPoints.Add(detectable.GetClosestPoint(sensorPos));
                                break;

                            case ColliderDetectionType.Shape:
                                m_TmpWorldPoints.AddRange(detectable.GetShapePoints(
                                    constraint.NormalizeDistance(
                                        worldToLocalMatrix.MultiplyPoint3x4(
                                            detectable.GetPosition()))));
                                break;
                        }

                        for (int j = 0, c = m_TmpWorldPoints.Count; j < c; j++)
                        {
                            if (constraint.ContainsPoint(
                                worldToLocalMatrix.MultiplyPoint3x4(m_TmpWorldPoints[j]), 
                                out Vector3 normalized))
                            {
                                m_TmpNormPoints.Add(normalized);
                            }
                        }

                        if (m_TmpNormPoints.Count > 0)
                        {
                            Result.Add(detectable, m_TmpNormPoints);
                        }
                    }
                }
            }
        }

        public override void Reset()
        {
            if (m_ClearCacheOnReset)
            {
                DetectableGameObject.ClearCache();
            }
        }
    }

    public abstract class Constraint
    {
        public abstract bool ContainsPoint(Vector3 localPoint, out Vector3 normalized);
        public virtual float NormalizeDistance(Vector3 localPoint) => 0;
    }
}
