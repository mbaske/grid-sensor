using UnityEngine;
using System;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Fills space between points by downsampling points.
    /// </summary>
    public class PointModDownsampling : PointModifier
    {
        private float[,] m_Samples;

        /// <inheritdoc/>
        public override void Initialize(GridBuffer buffer)
        {
            base.Initialize(buffer);

            m_Samples = new float[
                Mathf.CeilToInt(buffer.Width / 2f),
                Mathf.CeilToInt(buffer.Height / 2f)];
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
            Array.Clear(m_Samples, 0, m_Samples.Length);
        }

        /// <inheritdoc/>
        public override void Process(GridBuffer buffer, int channel)
        {
            // Side length of squares around points required for filling 
            // Width x Height, IF positions were distributed evenly. 
            float sparseness = Mathf.Sqrt(Width * Height / (float)GridPositions.Count);

            if (sparseness > 1)
            {
                // Make downsample area a bit larger than
                // sparseness value in order to prevent gaps.
                int size = Mathf.RoundToInt(sparseness) + 2; // TBD pad

                // Bottom/left offset.
                int xOffset = m_xMin - (size - Width % size) / 2;
                int yOffset = m_yMin - (size - Height % size) / 2;

                int nx = 0, ny = 0;
                foreach (Vector2Int point in GridPositions)
                {
                    // Downsampling: grid pos -> sample pos.
                    int xSample = (point.x - xOffset) / size;
                    int ySample = (point.y - yOffset) / size;

                    nx = Mathf.Max(nx, xSample);
                    ny = Mathf.Max(ny, ySample);

                    m_Samples[xSample, ySample] = Mathf.Max(
                        m_Samples[xSample, ySample], buffer.Read(channel, point));
                }

                // Replace grid points with squares (size x size).
                GridPositions.Clear();

                for (int xSample = 0; xSample <= nx; xSample++)
                {
                    for (int ySample = 0; ySample <= ny; ySample++)
                    {
                        float sampleValue = m_Samples[xSample, ySample];

                        if (sampleValue > 0)
                        {
                            // Upscaling: sample pos -> grid pos.
                            int xGrid = xOffset + xSample * size;
                            int yGrid = yOffset + ySample * size;

                            for (int x = 0; x < size; x++)
                            {
                                for (int y = 0; y < size; y++)
                                {
                                    Vector2Int gridPos = new Vector2Int(xGrid + x, yGrid + y);
                                    // TryRead -> Square might go beyond grid size.
                                    if (buffer.TryRead(channel, gridPos, out float bufferValue))
                                    {
                                        // Occlusion, 3D specific:
                                        // Write maximum buffer value if already set.
                                        buffer.Write(channel, gridPos, Mathf.Max(bufferValue, sampleValue));
                                        // Store for later Write(buffer, channel, value) call.
                                        GridPositions.Add(gridPos);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // else: keep original positions, they're dense enough.
        }
    }
}