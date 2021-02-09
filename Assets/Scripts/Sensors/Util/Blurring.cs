using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors.Util
{
    public class Blurring
    {
        private RectInt m_BlurArea;
        private readonly float m_Strength;
        private readonly float m_Threshold;
        private readonly ChannelGrid m_DataGrid;
        private readonly ChannelGrid m_BlurGrid;

        public Blurring(ChannelGrid dataGrid, float strength, float threshold)
        {
            m_DataGrid = dataGrid;
            m_BlurGrid = new ChannelGrid(2, dataGrid.Width, dataGrid.Height);
            m_Strength = strength;
            m_Threshold = threshold;
            s_Brushes ??= CreateBrushes();
        }

        public void NewBlur(RectInt area)
        {
            m_BlurArea = area;
            m_BlurGrid.Clear();
        }

        public void BlurPoint(Vector2Int gridPos, int dataChannel, float sizeFactor)
        {
            var brush = GetBrush(sizeFactor);
            float dataValue = m_DataGrid.Read(dataChannel, gridPos);

            foreach (BlurAmount blur in brush)
            {
                Vector2Int p = gridPos + blur.Offset;
                if (m_BlurGrid.TryRead(0, p, out float blurValue) && blur.Value > blurValue)
                {
                    m_BlurGrid.Write(0, p, blur.Value);
                    // Store max data value for each blur pos.
                    m_BlurGrid.Write(1, p, Mathf.Max(m_BlurGrid.Read(1, p), dataValue));
                    // Expand area.
                    m_BlurArea.min = Vector2Int.Min(m_BlurArea.min, p);
                    m_BlurArea.max = Vector2Int.Max(m_BlurArea.max, p);
                }
            }
        }

        public void ApplyBlur(int dataChannel, int onehotChannel = -1)
        {
            bool onehot = onehotChannel != -1;

            for (int x = m_BlurArea.xMin; x <= m_BlurArea.xMax; x++)
            {
                for (int y = m_BlurArea.yMin; y <= m_BlurArea.yMax; y++)
                {
                    if (m_BlurGrid.TryRead(0, x, y, out float value) && value > m_Threshold)
                    {
                        // Copy data back from blur grid to data grid.
                        m_DataGrid.Write(dataChannel, x, y, m_BlurGrid.Read(1, x, y));

                        if (onehot)
                        {
                            m_DataGrid.Write(onehotChannel, x, y, 1);
                        }
                    }
                }
            }
        }

        private HashSet<BlurAmount> GetBrush(float sizeFactor)
        {
            return s_Brushes[(int)Mathf.Min(m_Strength * sizeFactor, c_MaxRadius - 1)];
        }

        // Brushes.

        private struct BlurAmount
        {
            public Vector2Int Offset;
            public float Value;
        }
        private const int c_MaxRadius = 12;
        private static HashSet<BlurAmount>[] s_Brushes;

        private static HashSet<BlurAmount>[] CreateBrushes()
        {
            var brushes = new HashSet<BlurAmount>[c_MaxRadius];

            for (int r = 2, n = c_MaxRadius + 1; r <= n; r++)
            {
                int i = r - 2;
                brushes[i] = new HashSet<BlurAmount>();

                for (int x = -r; x <= r; x++)
                {
                    for (int y = -r; y <= r; y++)
                    {
                        float value = 1 - new Vector2(x, y).magnitude / r;

                        if (value > 0)
                        {
                            brushes[i].Add(new BlurAmount
                            {
                                Offset = new Vector2Int(x, y),
                                Value = value
                            });
                        }
                    }
                }
            }

            return brushes;
        }
    }
}