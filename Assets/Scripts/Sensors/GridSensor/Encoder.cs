using System.Collections.Generic;
using UnityEngine;
using MBaske.Sensors.Util;

namespace MBaske.Sensors
{
    public abstract class Encoder
    {
        protected readonly PixelGrid m_Grid;
        protected readonly IEnumerable<string> m_Tags;
        protected readonly Blurring m_Blurring;
        protected readonly bool m_ApplyBlur;
        protected int m_CrntStackIndex;

        public Encoder(PixelGrid grid, IEnumerable<string> tags, float blurStrength, float blurThreshold)
        {
            m_Grid = grid;
            m_Tags = tags;

            if (blurStrength > 0)
            {
                m_ApplyBlur = true;
                m_Blurring = new Blurring(m_Grid, blurStrength, blurThreshold);
            }
        }

        public virtual void Reset()
        {
            m_CrntStackIndex = 0;
        }

        public virtual void Encode(DetectionResult result)
        {
            // Implement in subclass.

            m_CrntStackIndex = ++m_CrntStackIndex % m_Grid.Shape.StackSize;
        }
    }
}