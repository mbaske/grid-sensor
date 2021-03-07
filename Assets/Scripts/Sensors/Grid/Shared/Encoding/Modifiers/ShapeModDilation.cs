using UnityEngine;
using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    // NOTE This is the most expensive modifier.
    public class ShapeModDilation : ShapeModifier
    {
        private struct Point
        {
            public int x;
            public int y;
            public float value;
        }

        // TBD
        private const int c_MaxRadius = 16;
        private const float c_SizeMult = 2;
        private const float c_Threshold = 0.2f;

        private ChannelGrid m_AuxGrid;
        private List<Point>[] m_Kernels;

        public override void Initialize(ChannelGrid grid)
        {
            base.Initialize(grid);

            // 0: point z
            // 1: kernel value (max)
            // 2: dilated z (max)
            m_AuxGrid = new ChannelGrid(3, grid.Width, grid.Height);
            m_Kernels = new List<Point>[c_MaxRadius];

            for (int r = 1; r <= c_MaxRadius; r++)
            {
                int i = r - 1;
                m_Kernels[i] = new List<Point>(r * r * 4);

                for (int x = -r; x <= r; x++)
                {
                    for (int y = -r; y <= r; y++)
                    {
                        float value = 1 - new Vector2(x, y).magnitude / r;

                        if (value > 0)
                        {
                            m_Kernels[i].Add(new Point
                            {
                                x = x,
                                y = y,
                                value = value
                            });
                        }
                    }
                }
            }
        }

        public override void Clear()
        {
            base.Clear();
            m_AuxGrid.Clear();
        }

        public override void AddPoint(Vector2Int p, float z)
        {
            base.AddPoint(p, z);
            m_AuxGrid.Write(0, p, z);
        }

        public override void Process(ChannelGrid grid, int channel)
        {
            float s = Mathf.Sqrt(Width * Height / (float)m_UniqueGridPoints.Count);

            if (s > 1)
            {
                s *= c_SizeMult;

                foreach (Vector2Int p in m_UniqueGridPoints)
                {
                    int gx = p.x;
                    int gy = p.y;
                    float v = grid.Read(channel, gx, gy);
                    float invZ = 1 - m_AuxGrid.Read(0, gx, gy);
                    var kernel = m_Kernels[Mathf.Min(Mathf.RoundToInt(s * invZ), c_MaxRadius - 1)];

                    for (int i = 0, n = kernel.Count; i < n; i++)
                    {
                        int x = gx + kernel[i].x;
                        int y = gy + kernel[i].y;

                        if (m_AuxGrid.TryRead(1, x, y, out float k))
                        {
                            m_AuxGrid.Write(1, x, y, Mathf.Max(kernel[i].value, k));
                            m_AuxGrid.Write(2, x, y, Mathf.Max(m_AuxGrid.Read(2, x, y), v));
                            Expand(x, y);
                        }
                    }
                }

                for (int x = m_xMin; x <= m_xMax; x++)
                {
                    for (int y = m_yMin; y <= m_yMax; y++)
                    {
                        if (m_AuxGrid.Read(1, x, y) > c_Threshold)
                        {
                            grid.Write(channel, x, y, m_AuxGrid.Read(2, x, y));
                            m_UniqueGridPoints.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }
        }
    }
}