using System.Collections.Generic;
using UnityEngine;
using MBaske.Sensors.Util;

namespace MBaske.Sensors
{
    public enum ColliderDetectionType
    {
        Position, ClosestPoint, Shape
    }

    public enum DistanceNormalization
    {
        Linear, Weighted
    }

    public struct DetectedCollider : IAdditionalDetectionData
    {
        public Vector3 Position; // world
        public Collider Collider;
    }

    /// <summary>
    /// Detects colliders with specified layers
    /// and tags within the sensor's field of view.
    /// Generates a <see cref="DetectionResult"/>.
    /// </summary>
    public class ColliderDetector : Detector
    {
        private readonly Collider[] m_Buffer;
        private readonly Transform m_ReferenceFrame;
        private readonly int m_LayerMask;
        private readonly Rect m_LonLatRect;
        private readonly float m_MinDistance;
        private readonly float m_MinDistanceSqr;
        private readonly float m_MaxDistance;
        private readonly float m_MaxDistanceSqr;
        private readonly float m_DistanceRange;
        private readonly float m_NormalizationWeight;
        private readonly bool m_ApplyWeight;
        private readonly bool m_DetectShape;
        private readonly bool m_DetectClosest;
        private readonly bool m_ClearCache;
        private readonly float m_ScanResolution;
        private readonly float m_ScanExtent;

        // Stats
        private int m_ColliderCount;
        private int m_TotalPointCount;
        private int m_VisiblePointCount;

        public override string Stats() => string.Format(
            "{0} of {1} points in {2} colliders", 
            m_VisiblePointCount, m_TotalPointCount, m_ColliderCount);

        // NOTE Each collider is scanned once and then cached. Therefore multiple sensors 
        // detecting identical colliders shouldn't have different scan settings. If they do, 
        // then the first sensor decides how scanning is done and the settings of later sensors 
        // will be ignored.
        private static readonly Dictionary<Collider, ColliderShape> s_SharedColliderShapeCache
            = new Dictionary<Collider, ColliderShape>();

        public ColliderDetector(
            Transform referenceFrame,
            int bufferSize,
            int layerMask,
            IEnumerable<string> tags,
            Rect lonLatRect,
            float minDistance,
            float maxDistance,
            float normalizationWeight,
            DistanceNormalization distanceNormalization,
            ColliderDetectionType detectionType,
            float scanResolution,
            float scanExtent,
            bool clearCache)
            : base(tags)
        {
            m_ReferenceFrame = referenceFrame;
            m_Buffer = new Collider[bufferSize];
            m_LayerMask = layerMask;
            m_LonLatRect = lonLatRect;

            m_MinDistance = minDistance;
            m_MinDistanceSqr = minDistance * minDistance;
            m_MaxDistance = maxDistance;
            m_MaxDistanceSqr = maxDistance * maxDistance;

            m_DistanceRange = maxDistance - minDistance;
            m_NormalizationWeight = normalizationWeight;
            m_ApplyWeight = distanceNormalization == DistanceNormalization.Weighted;

            m_DetectClosest = detectionType == ColliderDetectionType.ClosestPoint;
            m_DetectShape = detectionType == ColliderDetectionType.Shape;
            m_ScanResolution = scanResolution;
            m_ScanExtent = scanExtent;
            m_ClearCache = clearCache;

            s_SharedColliderShapeCache.Clear();
        }

        /// <summary>
        /// Resets the detector. 
        /// Optionally clears the collider shape cache.
        /// </summary>
        public override void Reset()
        {
            if (m_ClearCache)
            {
                s_SharedColliderShapeCache.Clear();
            }
        }

        /// <summary>
        /// Searches for colliders and calulates their FOV coordinates.
        /// <returns>An updated <see cref="DetectionResult"/>.</returns>
        /// </summary>
        public override DetectionResult Update()
        {
            Result.Clear();

            var center = m_ReferenceFrame.position;
            m_ColliderCount = Physics.OverlapSphereNonAlloc(center, m_MaxDistance, m_Buffer, m_LayerMask);
            m_TotalPointCount = 0;
            m_VisiblePointCount = 0;

            if (m_ColliderCount > 0)
            {
                for (int i = 0; i < m_ColliderCount; i++)
                {
                    if (m_Tags.Contains(m_Buffer[i].tag))
                    {
                        if (m_DetectShape)
                        {
                            DetectShape(center, m_Buffer[i]);
                        }
                        else
                        {
                            DetectPoint(center, m_Buffer[i]);
                        }
                    }
                }
            }

            return Result;
        }

        private void DetectPoint(Vector3 center, Collider cld)
        {
            var pos = m_DetectClosest
                ? cld.ClosestPoint(center)
                : cld.transform.position;

            if (IsVisiblePoint(center, pos, out NormalizedPoint point))
            {
                var item = Result.NewDetectionDataItem();
                item.AdditionalDetectionData = new DetectedCollider
                {
                    Position = pos,
                    Collider = cld
                };
                item.Tag = cld.tag;
                item.AddPoint(point);
                Result.AddDetectionDataItem(item);
                m_VisiblePointCount++;
            }
            m_TotalPointCount++;
        }

        private void DetectShape(Vector3 center, Collider cld)
        {
            var pos = cld.ClosestPoint(center);

            if (IsVisiblePoint(center, pos, out NormalizedPoint point))
            {
                // Should always be true. We want at least the
                // closest point's coords, even if all scanned 
                // points happen to be outside the field of view.
                var item = Result.NewDetectionDataItem();
                item.AdditionalDetectionData = new DetectedCollider
                {
                    Position = pos,
                    Collider = cld
                };
                item.Tag = cld.tag;
                item.AddPoint(point);
                m_VisiblePointCount++;
                m_TotalPointCount++;

                if (!s_SharedColliderShapeCache.TryGetValue(cld, out ColliderShape shape))
                {
                    shape = new ColliderShape(cld);
                    shape.Scan(m_ScanResolution, m_ScanExtent);
                    s_SharedColliderShapeCache.Add(cld, shape);
                }

                var shapePoints = shape.GetWorldPoints();
                foreach (Vector3 p in shapePoints)
                {
                    if (IsVisiblePoint(center, p, out point))
                    {
                        item.AddPoint(point);
                        m_VisiblePointCount++;
                    }
                }
                Result.AddDetectionDataItem(item);
                m_TotalPointCount += shape.NumPoints;
            }
        }

        private bool IsVisiblePoint(Vector3 center, Vector3 pos, out NormalizedPoint point)
        {
            var delta = pos - center;
            var sqrMag = delta.sqrMagnitude;

            if (sqrMag >= m_MinDistanceSqr && sqrMag <= m_MaxDistanceSqr)
            {
                var lonLat = Geometry.GetLonLat(m_ReferenceFrame, delta);
                if (m_LonLatRect.Contains(lonLat))
                {
                    float d = Mathf.Sqrt(sqrMag) - m_MinDistance; // > 0
                    // lon/lat -> norm. x/y
                    point.Position = Rect.PointToNormalized(m_LonLatRect, lonLat);
                    point.Position.z = d / m_DistanceRange;
                    point.Position.z = m_ApplyWeight
                        ? Normalization.InvSigmoid(point.Position.z, m_NormalizationWeight)
                        : 1 - point.Position.z; // 0 at max, 1 at min distance
                    point.DistanceRatio = m_DistanceRange / d; // 1 at max distance

                    return true;
                }
            }

            point = default;
            return false;
        }
    }
}
