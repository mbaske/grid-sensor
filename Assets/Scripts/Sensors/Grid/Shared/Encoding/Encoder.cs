using UnityEngine;

namespace MBaske.Sensors.Grid
{
    public abstract class Encoder
    {
        public PixelGrid Grid
        {
            set 
            { 
                m_Grid = value;
                m_GridSize = value.Size;
                m_StackSize = value.Shape.StackSize;
                m_NumChannels = value.Shape.ChannelsPerStackLayer;
            }
        }
        protected PixelGrid m_Grid;
        protected Vector2Int m_GridSize;
        protected int m_StackSize;
        protected int m_NumChannels;
        protected int m_CrntStackIndex;

        public abstract void Encode(DetectionResult result);

        public virtual void Reset()
        {
            m_Grid.Clear();
            m_CrntStackIndex = 0;
        }

        protected void IncrementStackIndex()
        {
            m_CrntStackIndex = ++m_CrntStackIndex % m_StackSize;
        }
    }
}