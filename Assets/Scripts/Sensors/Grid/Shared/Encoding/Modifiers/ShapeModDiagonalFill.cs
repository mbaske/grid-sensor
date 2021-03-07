using UnityEngine;

namespace MBaske.Sensors.Grid
{
    // TODO BUG
    // Fill is drawn over the entire grid width, if object is right
    // behind sensor, with points on both sides of -180/+180 divide.
    public class ShapeModDiagonalFill : ShapeModifier
    {
        public override void Process(ChannelGrid grid, int channel)
        {
            if (Width > Height)
            {
                int cx = (m_xMin + m_xMax) / 2;
                float value = 0;

                for (int x = m_xMin + 1; x <= cx; x++)
                {
                    for (int y = m_yMin; y <= m_yMax; y++)
                    {
                        float v = grid.Read(channel, x, y);

                        if (v == 0)
                        {
                            int xp = x - 1;
                            v = grid.Read(channel, xp, y);

                            if (y > m_yMin)
                            {
                                v = Mathf.Max(v, grid.Read(channel, xp, y - 1));
                            }
                            if (y < m_yMax)
                            {
                                v = Mathf.Max(v, grid.Read(channel, xp, y + 1));
                            }

                            if (v > 0)
                            {
                                grid.Write(channel, x, y, v);
                                m_UniqueGridPoints.Add(new Vector2Int(x, y));
                                value = v;
                            }
                        }
                        else
                        {
                            grid.Write(channel, x, y, Mathf.Max(value, v));
                        }
                    }
                }

                value = 0;

                for (int x = m_xMax - 1; x > cx; x--)
                {
                    for (int y = m_yMin; y <= m_yMax; y++)
                    {
                        float v = grid.Read(channel, x, y);

                        if (v == 0)
                        {
                            int xp = x + 1;
                            v = grid.Read(channel, xp, y);

                            if (y > m_yMin)
                            {
                                v = Mathf.Max(v, grid.Read(channel, xp, y - 1));
                            }
                            if (y < m_yMax)
                            {
                                v = Mathf.Max(v, grid.Read(channel, xp, y + 1));
                            }

                            if (v > 0)
                            {
                                grid.Write(channel, x, y, v);
                                m_UniqueGridPoints.Add(new Vector2Int(x, y));
                                value = v;
                            }
                        }
                        else
                        {
                            grid.Write(channel, x, y, Mathf.Max(value, v));
                        }
                    }
                }
            }
            else
            {
                int cy = (m_yMin + m_yMax) / 2;
                float value = 0;

                for (int y = m_yMin + 1; y <= cy; y++)
                {
                    for (int x = m_xMin; x <= m_xMax; x++)
                    {
                        float v = grid.Read(channel, x, y);

                        if (v == 0)
                        {
                            int yp = y - 1;
                            v = grid.Read(channel, x, yp);

                            if (x > m_xMin)
                            {
                                v = Mathf.Max(v, grid.Read(channel, x - 1, yp));
                            }
                            if (x < m_xMax)
                            {
                                v = Mathf.Max(v, grid.Read(channel, x + 1, yp));
                            }

                            if (v > 0)
                            {
                                grid.Write(channel, x, y, v);
                                m_UniqueGridPoints.Add(new Vector2Int(x, y));
                                value = v;
                            }
                        }
                        else
                        {
                            grid.Write(channel, x, y, Mathf.Max(value, v));
                        }
                    }
                }

                value = 0;

                for (int y = m_yMax - 1; y > cy; y--)
                {
                    for (int x = m_xMin; x <= m_xMax; x++)
                    {
                        float v = grid.Read(channel, x, y);

                        if (v == 0)
                        {
                            int yp = y + 1;
                            v = grid.Read(channel, x, yp);

                            if (x > m_xMin)
                            {
                                v = Mathf.Max(v, grid.Read(channel, x - 1, yp));
                            }
                            if (x < m_xMax)
                            {
                                v = Mathf.Max(v, grid.Read(channel, x + 1, yp));
                            }

                            if (v > 0)
                            {
                                grid.Write(channel, x, y, v);
                                m_UniqueGridPoints.Add(new Vector2Int(x, y));
                                value = v;
                            }
                        }
                        else
                        {
                            grid.Write(channel, x, y, Mathf.Max(value, v));
                        }
                    }
                }
            }
        }
    }
}