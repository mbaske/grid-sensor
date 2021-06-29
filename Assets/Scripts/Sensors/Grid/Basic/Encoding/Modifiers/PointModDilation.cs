using UnityEngine;
using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Fills space between points by applying point dilation.
    /// </summary>
    public class PointModDilation : PointModifier
    {
        private struct Point
        {
            public int x;
            public int y;
            public float value;
        }

        // TBD dilation settings.
        // Maximum kernel radius.
        private const int c_MaxRadius = 16;
        // Multiplier for picking kernel size.
        private const float c_Multiplier = 1.25f;
        // Include grid pos if kernel value > threshold.
        private const float c_Threshold = 0.25f;

        private GridBuffer m_AuxGrid;
        private List<Point>[] m_Kernels;

        /// <inheritdoc/>
        public override void Initialize(GridBuffer buffer)
        {
            base.Initialize(buffer);

            // Helper GridBuffer, channels:
            // 0 - proximity lookup
            // 1 - kernel value (max, dilated area)
            // 2 - buffer value (max, dilated area)
            m_AuxGrid = new GridBuffer(3, buffer.Width, buffer.Height);
            m_Kernels = new List<Point>[c_MaxRadius];

            for (int radius = 1; radius <= c_MaxRadius; radius++)
            {
                int i = radius - 1;
                m_Kernels[i] = new List<Point>(radius * radius * 4); // TBD

                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        float value = 1 - new Vector2(x, y).magnitude / radius;

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

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            m_AuxGrid.Clear();
        }

        /// <inheritdoc/>
        public override void AddPosition(Vector2Int point, float proximity)
        {
            base.AddPosition(point, proximity);
            m_AuxGrid.Write(0, point, proximity);
        }

        /// <inheritdoc/>
        public override void Process(GridBuffer buffer, int channel)
        {
            // Side length of squares around points required for filling 
            // Width x Height, IF positions were distributed evenly. 
            float sparseness = Mathf.Sqrt(Width * Height / (float)GridPositions.Count);

            if (sparseness > 1)
            {
                sparseness *= c_Multiplier;

                foreach (Vector2Int pGrid in GridPositions)
                {
                    int xGrid = pGrid.x;
                    int yGrid = pGrid.y;
                    
                    float proximity = m_AuxGrid.Read(0, xGrid, yGrid);
                    // Get matching kernel size for current proximity and point sparseness.
                    var kernel = m_Kernels[Mathf.Min(Mathf.RoundToInt(sparseness * proximity), c_MaxRadius - 1)];

                    float bufferValue = buffer.Read(channel, xGrid, yGrid);

                    foreach (var pKernel in kernel)
                    {
                        int xDilate = xGrid + pKernel.x;
                        int yDilate = yGrid + pKernel.y;

                        // TryRead -> Dilation area might go beyond grid size.
                        if (m_AuxGrid.TryRead(1, xDilate, yDilate, out float kernelValue))
                        {
                            // Occlusion, 3D specific:
                            // Write maximum kernel value if already set.
                            m_AuxGrid.Write(1, xDilate, yDilate, Mathf.Max(pKernel.value, kernelValue));
                            // Write maximum buffer value if already set.
                            m_AuxGrid.Write(2, xDilate, yDilate, Mathf.Max(m_AuxGrid.Read(2, xDilate, yDilate), bufferValue));
                            Expand(xDilate, yDilate);
                        }
                    }
                }

                // Expanded area.
                for (int x = m_xMin; x <= m_xMax; x++)
                {
                    for (int y = m_yMin; y <= m_yMax; y++)
                    {
                        if (m_AuxGrid.Read(1, x, y) > c_Threshold)
                        {
                            // Copy back dilated buffer values.
                            buffer.Write(channel, x, y, m_AuxGrid.Read(2, x, y));
                            // Store for later Write(buffer, channel, value) call.
                            GridPositions.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }
            // else: keep original positions, they're dense enough.
        }
    }
}