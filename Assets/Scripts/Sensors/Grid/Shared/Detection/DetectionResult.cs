using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// DetectionResult for Detector -> Encoder logic.
    /// </summary>
    public class DetectionResult
    {
        public class Item
        {
            public IDetectable Detectable;
            public List<Vector3> Points = new List<Vector3>();
        }

        public IList<string> DetectableTags { get; private set; }

        private readonly Stack<Item> m_ItemPool;
        private readonly IList<IDetectable> m_Detectables;
        private readonly IDictionary<string, IList<Item>> m_ItemsByTag;

        public DetectionResult(IList<string> detectableTags, int bufferSize)
        {
            DetectableTags = new List<string>(detectableTags);
            m_Detectables = new List<IDetectable>(bufferSize);
            m_ItemPool = new Stack<Item>(bufferSize);
            m_ItemsByTag = new Dictionary<string, IList<Item>>(DetectableTags.Count);
            
            for (int i = 0, n = DetectableTags.Count; i < n; i++)
            {
                m_ItemsByTag.Add(DetectableTags[i], new List<Item>(bufferSize));
            }
        }

        public void Clear()
        {
            m_Detectables.Clear();
            foreach (var list in m_ItemsByTag.Values)
            {
                for (int i = 0, n = list.Count; i < n; i++)
                {
                    list[i].Points.Clear();
                    m_ItemPool.Push(list[i]);
                }
                list.Clear();
            }
        }

        public void Add(IDetectable detectable, IList<Vector3> points)
        {
            Item item = m_ItemPool.Count > 0 ? m_ItemPool.Pop() : new Item();
            item.Detectable = detectable;
            item.Points.AddRange(points);
            m_ItemsByTag[detectable.Tag].Add(item);
            m_Detectables.Add(detectable);
        }

        public bool TryGetItems(string tag, out IList<Item> items)
        {
            if (m_ItemsByTag.TryGetValue(tag, out items) && items.Count > 0)
            {
                return true;
            }

            items = null;
            return false;
        }

        public bool Contains(IDetectable detectable)
        {
            return m_Detectables.Contains(detectable);
        }

        public int Count(string tag)
        {
            if (m_ItemsByTag.TryGetValue(tag, out IList<Item> items))
            {
                return items.Count;
            }

            return 0;
        }

        public int Count()
        {
            int sum = 0;
            foreach (var list in m_ItemsByTag.Values)
            {
                sum += list.Count;
            }

            return sum;
        }
    }
}