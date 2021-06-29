using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Fills space between points by drawing orthogonal lines.
    /// </summary>
    // TODO BUG
    // Fill is drawn over the entire grid width, if object is right
    // behind sensor, with points on both sides of -180/+180 divide.
    public class PointModOrthogonalFill : PointModifier
    {
        /// <inheritdoc/>
        public override void Process(GridBuffer buffer, int channel)
        {
            int xCenter = (m_xMin + m_xMax) / 2;
            int yCenter = (m_yMin + m_yMax) / 2;

            float tmpValue = 0;

            for (int x = m_xMin; x <= xCenter; x++)
            {
                int xLeft = Mathf.Max(m_xMin, x - 1);

                for (int y = m_yMin; y <= yCenter; y++)
                {
                    float bufferValue = buffer.Read(channel, x, y);

                    if (bufferValue == 0)
                    {
                        int yTop = Mathf.Max(m_yMin, y - 1);

                        bufferValue = Mathf.Max(
                            buffer.Read(channel, xLeft, y),
                            buffer.Read(channel, x, yTop),
                            buffer.Read(channel, xLeft, yTop));

                        if (bufferValue > 0)
                        {
                            buffer.Write(channel, x, y, bufferValue);
                            tmpValue = bufferValue;
                            // Store for later Write(buffer, channel, value) call.
                            GridPositions.Add(new Vector2Int(x, y));
                        }
                    }
                    else
                    {
                        // Occlusion, 3D specific:
                        // Write maximum buffer value if already set.
                        buffer.Write(channel, x, y, Mathf.Max(tmpValue, bufferValue));
                    }
                }
            }

            tmpValue = 0;

            for (int x = m_xMax ; x > xCenter; x--)
            {
                int xRight = Mathf.Min(m_xMax, x + 1);

                for (int y = m_yMin ; y <= yCenter; y++)
                {
                    float bufferValue = buffer.Read(channel, x, y);

                    if (bufferValue == 0)
                    {
                        int yTop = Mathf.Max(m_yMin, y - 1);

                        bufferValue = Mathf.Max(
                            buffer.Read(channel, xRight, y),
                            buffer.Read(channel, x, yTop),
                            buffer.Read(channel, xRight, yTop));

                        if (bufferValue > 0)
                        {
                            buffer.Write(channel, x, y, bufferValue);
                            tmpValue = bufferValue;
                            // Store for later Write(buffer, channel, value) call.
                            GridPositions.Add(new Vector2Int(x, y));
                        }
                    }
                    else
                    {
                        // Occlusion, 3D specific:
                        // Write maximum buffer value if already set.
                        buffer.Write(channel, x, y, Mathf.Max(tmpValue, bufferValue));
                    }
                }
            }

            tmpValue = 0;

            for (int x = m_xMin ; x <= xCenter; x++)
            {
                int xLeft = Mathf.Max(m_xMin, x - 1);

                for (int y = m_yMax ; y > yCenter; y--)
                {
                    float bufferValue = buffer.Read(channel, x, y);

                    if (bufferValue == 0)
                    {
                        int yBottom = Mathf.Min(m_yMax, y + 1);

                        bufferValue = Mathf.Max(
                            buffer.Read(channel, xLeft, y),
                            buffer.Read(channel, x, yBottom),
                            buffer.Read(channel, xLeft, yBottom));

                        if (bufferValue > 0)
                        {
                            buffer.Write(channel, x, y, bufferValue);
                            tmpValue = bufferValue;
                            // Store for later Write(buffer, channel, value) call.
                            GridPositions.Add(new Vector2Int(x, y));
                        }
                    }
                    else
                    {
                        // Occlusion, 3D specific:
                        // Write maximum buffer value if already set.
                        buffer.Write(channel, x, y, Mathf.Max(tmpValue, bufferValue));
                    }
                }
            }

            tmpValue = 0;

            for (int x = m_xMax ; x > xCenter; x--)
            {
                int xRight = Mathf.Min(m_xMax, x + 1);

                for (int y = m_yMax ; y > yCenter; y--)
                {
                    float bufferValue = buffer.Read(channel, x, y);

                    if (bufferValue == 0)
                    {
                        int yBottom = Mathf.Min(m_yMax, y + 1);

                        bufferValue = Mathf.Max(
                            buffer.Read(channel, xRight, y),
                            buffer.Read(channel, x, yBottom),
                            buffer.Read(channel, xRight, yBottom));

                        if (bufferValue > 0)
                        {
                            buffer.Write(channel, x, y, bufferValue);
                            tmpValue = bufferValue;
                            // Store for later Write(buffer, channel, value) call.
                            GridPositions.Add(new Vector2Int(x, y));
                        }
                    }
                    else
                    {
                        // Occlusion, 3D specific:
                        // Write maximum buffer value if already set.
                        buffer.Write(channel, x, y, Mathf.Max(tmpValue, bufferValue));
                    }
                }
            }
        }
    }
}