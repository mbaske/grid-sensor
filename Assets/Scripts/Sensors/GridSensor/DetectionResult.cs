using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors
{
    public class DetectionResult
    {
        private readonly Dictionary<string, IList<DetectionData>> m_Dict;
        private readonly Stack<DetectionData> m_Pool;

        public DetectionResult(IEnumerable<string> tags)
        {
            m_Pool = new Stack<DetectionData>();
            m_Dict = new Dictionary<string, IList<DetectionData>>();

            foreach (var tag in tags)
            {
                m_Dict.Add(tag, new List<DetectionData>());
            }
        }

        public void Clear()
        {
            foreach (var list in m_Dict.Values)
            {
                foreach (var item in list)
                {
                    item.Clear();
                    m_Pool.Push(item);
                }

                list.Clear();
            }
        }

        public DetectionData NewDetectionDataItem()
        {
            if (m_Pool.Count > 0)
            {
                return m_Pool.Pop();
            }

            return new DetectionData();
        }

        public void AddDetectionDataItem(DetectionData item)
        {
            if (m_Dict.TryGetValue(item.Tag, out IList<DetectionData> list))
            {
                list.Add(item);
            }
            else
            {
                throw new KeyNotFoundException(item.Tag);
            }
        }

        public IList<DetectionData> GetDetectionDataList(string tag)
        {
            if (m_Dict.TryGetValue(tag, out IList<DetectionData> list))
            {
                return list;
            }

            throw new KeyNotFoundException(tag);
        }
    }
}
