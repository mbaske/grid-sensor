using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors
{
    public abstract class Detector
    {
        public DetectionResult Result { get; private set; }

        protected readonly List<string> m_Tags;

        public Detector(IEnumerable<string> tags)
        {
            Result = new DetectionResult(tags);
            m_Tags = tags.ToList();
        }

        public abstract DetectionResult Update();
        public abstract string Stats();
        public abstract void Reset();
    }
}
