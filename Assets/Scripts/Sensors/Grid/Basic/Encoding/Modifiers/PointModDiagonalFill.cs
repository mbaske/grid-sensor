using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Fills space between points by drawing diagonal lines.
    /// </summary>
    // TODO BUG
    // Fill is drawn over the entire grid width, if object is right
    // behind sensor, with points on both sides of -180/+180 divide.
    public class PointModDiagonalFill : PointModifier
    {
        /// <inheritdoc/>
        public override void Process(GridBuffer buffer, int channel)
        {
            if (Width > Height)
            {
                int xCenter = (m_xMin + m_xMax) / 2;
                float tmpValue = 0;

                for (int x = m_xMin + 1; x <= xCenter; x++)
                {
                    for (int y = m_yMin; y <= m_yMax; y++)
                    {
                        float bufferValue = buffer.Read(channel, x, y);

                        if (bufferValue == 0)
                        {
                            int xLeft = x - 1;
                            bufferValue = buffer.Read(channel, xLeft, y);

                            if (y > m_yMin)
                            {
                                bufferValue = Mathf.Max(
                                    bufferValue, buffer.Read(channel, xLeft, y - 1));
                            }
                            if (y < m_yMax)
                            {
                                bufferValue = Mathf.Max(
                                    bufferValue, buffer.Read(channel, xLeft, y + 1));
                            }

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

                for (int x = m_xMax - 1; x > xCenter; x--)
                {
                    for (int y = m_yMin; y <= m_yMax; y++)
                    {
                        float bufferValue = buffer.Read(channel, x, y);

                        if (bufferValue == 0)
                        {
                            int xRight = x + 1;
                            bufferValue = buffer.Read(channel, xRight, y);

                            if (y > m_yMin)
                            {
                                bufferValue = Mathf.Max(
                                    bufferValue, buffer.Read(channel, xRight, y - 1));
                            }
                            if (y < m_yMax)
                            {
                                bufferValue = Mathf.Max(
                                    bufferValue, buffer.Read(channel, xRight, y + 1));
                            }

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
            else
            {
                int yCenter = (m_yMin + m_yMax) / 2;
                float tmpValue = 0;

                for (int y = m_yMin + 1; y <= yCenter; y++)
                {
                    for (int x = m_xMin; x <= m_xMax; x++)
                    {
                        float bufferValue = buffer.Read(channel, x, y);

                        if (bufferValue == 0)
                        {
                            int yTop = y - 1;
                            bufferValue = buffer.Read(channel, x, yTop);

                            if (x > m_xMin)
                            {
                                bufferValue = Mathf.Max(
                                    bufferValue, buffer.Read(channel, x - 1, yTop));
                            }
                            if (x < m_xMax)
                            {
                                bufferValue = Mathf.Max(
                                    bufferValue, buffer.Read(channel, x + 1, yTop));
                            }

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

                for (int y = m_yMax - 1; y > yCenter; y--)
                {
                    for (int x = m_xMin; x <= m_xMax; x++)
                    {
                        float bufferValue = buffer.Read(channel, x, y);

                        if (bufferValue == 0)
                        {
                            int yBottom = y + 1;
                            bufferValue = buffer.Read(channel, x, yBottom);

                            if (x > m_xMin)
                            {
                                bufferValue = Mathf.Max(
                                    bufferValue, buffer.Read(channel, x - 1, yBottom));
                            }
                            if (x < m_xMax)
                            {
                                bufferValue = Mathf.Max(
                                    bufferValue, buffer.Read(channel, x + 1, yBottom));
                            }

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
}