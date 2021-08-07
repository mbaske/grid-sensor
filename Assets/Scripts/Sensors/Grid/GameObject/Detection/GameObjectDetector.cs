using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Type of the detector space, box (2D) or sphere (3D).
    /// </summary>
    public enum DetectorSpaceType
    {
        Box, Sphere
    }

    /// <summary>
    /// Abstract base class for detecting gameobjects.
    /// </summary>
    public abstract class GameObjectDetector : IDetector
    {
        public DetectionResult Result
        {
            get { return m_Result; }
            set { m_Result = value; }
        }
        protected DetectionResult m_Result;

        /// <summary>
        /// The maximum number of colliders the detector can detect at once.
        /// </summary>
        public int ColliderBufferSize
        {
            set 
            { 
                m_ColliderBufferSize = value;
                m_ColliderBuffer = new Collider[m_ColliderBufferSize];
            }
        }
        protected int m_ColliderBufferSize;

        /// <summary>
        /// Whether to clear the <see cref="DetectableGameObject"/> 
        /// cache on sensor reset at the end of each episode.
        /// </summary>
        public bool ClearCacheOnReset
        {
            set { m_ClearCacheOnReset = value; }
        }
        protected bool m_ClearCacheOnReset;

        /// <summary>
        /// The sensor component's transform.
        /// </summary>
        public Transform SensorTransform
        {
            set { m_Transform = value; }
        }
        protected Transform m_Transform;

        /// <summary>
        /// Optional <see cref="DetectableGameObject"/> attached or 
        /// parented to the sensor's transform. This object will be  
        /// excluded from detection.
        /// </summary>
        public DetectableGameObject SensorOwner
        {
            set { m_Owner = value; }
        }
        protected DetectableGameObject m_Owner;

        /// <summary>
        /// The <see cref="GameObjectSettingsMeta"/> to use for detection.
        /// </summary>
        public GameObjectSettingsMeta Settings
        {
            set 
            {
                m_Settings = value;
                m_LayerMask = value.LayerMask;
            }
        }
        protected GameObjectSettingsMeta m_Settings;

        protected Collider[] m_ColliderBuffer;
        protected int m_LayerMask;

        // Temp. points, TODO init capacity?
        private readonly List<Vector3> m_NormPoints = new List<Vector3>(1024);
        private readonly List<Vector3> m_WorldPoints = new List<Vector3>(1024);


        protected void ValidateColliderBufferSize(int numFound)
        {
            if (numFound == m_ColliderBufferSize)
            {
                m_ColliderBufferSize *= 2;
                m_ColliderBuffer = new Collider[m_ColliderBufferSize];
                Debug.LogWarning("Doubling collider buffer size to " + m_ColliderBufferSize);
            }
        }

        protected void ParseColliders(
            int numFound,
            DetectionConstraint constraint,
            Matrix4x4 worldToLocalMatrix)
        {
            Vector3 sensorPos = m_Transform.position;

            for (int i = 0; i < numFound; i++)
            {
                if (m_Settings.IsDetectableTag(
                    m_ColliderBuffer[i].tag, 
                    out PointDetectionType type))
                {
                    var detectable = DetectableGameObject.GetCached(m_ColliderBuffer[i]);
                    // Need to filter out compound collider duplicate results,
                    // as each collider is a key in DetectableGameObject's cache.
                    // We also ignore the sensor owner if there is a  
                    // DetectableGameObject parent to the sensor.
                    if (detectable != m_Owner && !Result.Contains(detectable))
                    {
                        m_NormPoints.Clear();
                        m_WorldPoints.Clear();

                        switch (type)
                        {
                            case PointDetectionType.Position:
                                m_WorldPoints.Add(detectable.GetWorldPosition());
                                break;

                            case PointDetectionType.ClosestPoint:
                                m_WorldPoints.Add(detectable.GetClosestWorldPoint(sensorPos));
                                break;

                            case PointDetectionType.Shape:
                                float normDistance = constraint.GetNormalizedDistance(
                                        worldToLocalMatrix.MultiplyPoint3x4(
                                            detectable.GetClosestWorldPoint(sensorPos)));
                                m_WorldPoints.AddRange(
                                    detectable.GetShapeWorldPoints(normDistance));
                                break;
                        }

                        for (int j = 0, c = m_WorldPoints.Count; j < c; j++)
                        {
                            if (constraint.ContainsPoint(
                                worldToLocalMatrix.MultiplyPoint3x4(m_WorldPoints[j]), 
                                out Vector3 normPoint))
                            {
                                m_NormPoints.Add(normPoint);
                            }
                        }

                        if (m_NormPoints.Count > 0)
                        {
                            Result.Add(detectable, m_NormPoints);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        /// <summary>
        /// Detects colliders and gets the associated <see cref="DetectableGameObject"/>
        /// instances from the shared cache. Retrieves the objects' points as defined in 
        /// <see cref="PointDetectionType"/> settings and transforms them into the sensor
        /// component's frame of reference.
        /// </summary>
        public virtual void OnSensorUpdate() 
        {
            m_Result.Clear();
        }

        /// <summary>
        /// Invoked on sensor reset at the end of each episode.
        /// </summary>
        public void OnSensorReset()
        {
            if (m_ClearCacheOnReset)
            {
                DetectableGameObject.ClearCache();
            }
        }
    }
}
