using UnityEngine;
using System;

namespace MBaske.Sensors.Grid
{
    public class ShapeModDownsampling : ShapeModifier
    {
        private float[,] m_Samples;

        public override void Initialize(ChannelGrid grid)
        {
            base.Initialize(grid);

            m_Samples = new float[
                Mathf.CeilToInt(grid.Width / 2f),
                Mathf.CeilToInt(grid.Height / 2f)];
        }

        public override void Clear()
        {
            base.Clear();
            Array.Clear(m_Samples, 0, m_Samples.Length);
        }

        public override void Process(ChannelGrid grid, int channel)
        {
            // Side length of squares around points required for filling w * h,
            // *if* points were distributed evenly. We're adding some value
            // to this below for preventing gaps.
            int w = Width;
            int h = Height;
            float s = Mathf.Sqrt(w * h / (float)m_UniqueGridPoints.Count);

            if (s > 1)
            {
                int size = Mathf.RoundToInt(s) + 2; // TBD add. value

                // Bottom/left offset (padding).
                int xPad = m_xMin - (size - w % size) / 2;
                int yPad = m_yMin - (size - h % size) / 2;

                int nx = 0, ny = 0;
                foreach (Vector2Int p in m_UniqueGridPoints)
                {
                    int sx = (p.x - xPad) / size;
                    int sy = (p.y - yPad) / size;
                    nx = Mathf.Max(nx, sx);
                    ny = Mathf.Max(ny, sy);
                    // For 3D, we sample the max (closest) value. 
                    // For 2D, point values are identical anyway.
                    m_Samples[sx, sy] = Mathf.Max(m_Samples[sx, sy], grid.Read(channel, p));
                }

                // Replace grid points with squares (size x size).
                m_UniqueGridPoints.Clear();

                for (int sx = 0; sx <= nx; sx++)
                {
                    for (int sy = 0; sy <= ny; sy++)
                    {
                        float value = m_Samples[sx, sy];

                        if (value > 0)
                        {
                            int gx = xPad + sx * size;
                            int gy = yPad + sy * size;

                            for (int x = 0; x < size; x++)
                            {
                                for (int y = 0; y < size; y++)
                                {
                                    Vector2Int p = new Vector2Int(gx + x, gy + y);
                                    if (grid.TryRead(channel, p, out float prev))
                                    {
                                        // 3D: storing max value for distance channel (closest).
                                        // TODO
                                        // 2D: neighbouring down-sampled areas might overlap and
                                        // their max value isn't necessarily what we want to observe?
                                        grid.Write(channel, p, Mathf.Max(prev, value));
                                        m_UniqueGridPoints.Add(p);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // else: keep original points.
        }
    }
}