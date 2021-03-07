using UnityEngine;

namespace MBaske.Sensors.Grid
{
    // TODO BUG
    // Fill is drawn over the entire grid width, if object is right
    // behind sensor, with points on both sides of -180/+180 divide.
    public class ShapeModOrthogonalFill : ShapeModifier
    {
        public override void Process(ChannelGrid grid, int channel)
        {
            int cx = (m_xMin + m_xMax) / 2;
            int cy = (m_yMin + m_yMax) / 2;

            float value = 0;

            for (int x = m_xMin; x <= cx; x++)
            {
                int xp = Mathf.Max(m_xMin, x - 1);

                for (int y = m_yMin; y <= cy; y++)
                {
                    float v = grid.Read(channel, x, y);

                    if (v == 0)
                    {
                        int yp = Mathf.Max(m_yMin, y - 1);

                        v = Mathf.Max(
                            grid.Read(channel, xp, y),
                            grid.Read(channel, x, yp),
                            grid.Read(channel, xp, yp));

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

            for (int x = m_xMax ; x > cx; x--)
            {
                int xp = Mathf.Min(m_xMax, x + 1);

                for (int y = m_yMin ; y <= cy; y++)
                {
                    float v = grid.Read(channel, x, y);

                    if (v == 0)
                    {
                        int yp = Mathf.Max(m_yMin, y - 1);

                        v = Mathf.Max(
                            grid.Read(channel, xp, y),
                            grid.Read(channel, x, yp),
                            grid.Read(channel, xp, yp));

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

            for (int x = m_xMin ; x <= cx; x++)
            {
                int xp = Mathf.Max(m_xMin, x - 1);

                for (int y = m_yMax ; y > cy; y--)
                {
                    float v = grid.Read(channel, x, y);

                    if (v == 0)
                    {
                        int yp = Mathf.Min(m_yMax, y + 1);

                        v = Mathf.Max(
                            grid.Read(channel, xp, y),
                            grid.Read(channel, x, yp),
                            grid.Read(channel, xp, yp));

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

            for (int x = m_xMax ; x > cx; x--)
            {
                int xp = Mathf.Min(m_xMax, x + 1);

                for (int y = m_yMax ; y > cy; y--)
                {
                    float v = grid.Read(channel, x, y);

                    if (v == 0)
                    {
                        int yp = Mathf.Min(m_yMax, y + 1);

                        v = Mathf.Max(
                            grid.Read(channel, xp, y),
                            grid.Read(channel, x, yp),
                            grid.Read(channel, xp, yp));

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