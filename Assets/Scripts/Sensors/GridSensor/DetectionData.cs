using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors
{
    public struct NormalizedPoint
    {
        public Vector3 Position;
        // 1 at max distance, > 1 if closer.
        public float DistanceRatio;
    }

    public interface IAdditionalDetectionData { }

    public class DetectionData
    {
        public string Tag { get; set; }
        public Rect NormalizedRect => m_Rect;
        public IEnumerable<NormalizedPoint> NormalizedPoints => m_Points;
        public IAdditionalDetectionData AdditionalDetectionData { get; set; }

        private Rect m_Rect;
        private readonly HashSet<NormalizedPoint> m_Points;

        public DetectionData()
        {
            m_Points = new HashSet<NormalizedPoint>();
            Clear();
        }

        public void Clear()
        {
            m_Points.Clear();
            m_Rect.min = Vector2.one;
            m_Rect.max = Vector2.zero;
        }

        public void AddPoint(NormalizedPoint point)
        {
            m_Points.Add(point);
            m_Rect.min = Vector2.Min(m_Rect.min, point.Position);
            m_Rect.max = Vector2.Max(m_Rect.max, point.Position);
        }
    }
}