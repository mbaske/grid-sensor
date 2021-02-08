using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors
{
    public enum ChannelEncoding
    {
        DistancesOnly, OneHotAndDistances, OneHotAndShortestDistance
    }

    public interface IGridEncoder
    {
        void Reset();
        void Encode(DetectionResult result);
    }

    /// <summary>
    /// Encodes a <see cref="DetectionResult"/> in a <see cref="PixelGrid"/>.
    /// </summary>
    public class GridEncoder : IGridEncoder
    {
        private struct Blur
        {
            public Vector2Int Pos;
            public float Value;
        }

        private const int c_MaxBlurRadius = 12;
        private readonly bool m_ApplyBlur;
        private readonly float m_BlurStrength;
        private readonly float m_BlurThreshold;
        private readonly ChannelGrid m_BlurGrid;
        private HashSet<Blur>[] m_BlurBrushes;
        private RectInt m_BlurArea;

        private readonly PixelGrid m_ObsGrid;
        private readonly ChannelEncoding m_Encoding;
        private readonly IEnumerable<string> m_Tags;
        private readonly int m_ChannelOffset;
        private readonly int m_ChannelIncr;
        private int m_CrntStackIndex;

        public GridEncoder(
            PixelGrid grid,
            ChannelEncoding encoding,
            IEnumerable<string> tags,
            float blurStrength,
            float blurThreshold)
        {
            m_ObsGrid = grid;
            m_Encoding = encoding;
            m_Tags = tags;

            m_ChannelOffset = encoding == ChannelEncoding.OneHotAndShortestDistance ? 1 : 0;
            m_ChannelIncr = encoding == ChannelEncoding.OneHotAndDistances ? 2 : 1;

            m_ApplyBlur = blurStrength > 0;

            if (m_ApplyBlur)
            {
                CreateBlurBrushes();
                m_BlurStrength = blurStrength;
                m_BlurThreshold = blurThreshold;
                m_BlurGrid = new ChannelGrid(2, m_ObsGrid.Width, m_ObsGrid.Height);
            }
        }

        public void Reset()
        {
            m_CrntStackIndex = 0;
        }

        public void Encode(DetectionResult result)
        {
            int n = m_ObsGrid.Shape.ChannelsPerStackLayer;
            int channel = n * m_CrntStackIndex;
            m_ObsGrid.ClearChannels(channel, n);

            int shortestDistanceChannel = channel;
            channel += m_ChannelOffset;

            foreach (string tag in m_Tags)
            {
                var items = result.GetItems(tag);
                foreach (var item in items)
                {
                    switch (m_Encoding)
                    {
                        case ChannelEncoding.DistancesOnly:
                            EncodeItem(item, channel);
                            break;

                        case ChannelEncoding.OneHotAndShortestDistance:
                            EncodeItem(item, shortestDistanceChannel, channel);
                            break;

                        case ChannelEncoding.OneHotAndDistances:
                            EncodeItem(item, channel, channel + 1);
                            break;
                    }
                }

                channel += m_ChannelIncr;
            }

            m_CrntStackIndex = ++m_CrntStackIndex % m_ObsGrid.Shape.StackSize;
        }

        private void EncodeItem(DetectionResult.Item item, int distanceChannel, int onehotChannel = -1)
        {
            if (m_ApplyBlur)
            {
                m_BlurGrid.Clear();
                m_BlurArea = m_BlurGrid.NormalizedToGridRect(item.Rect);
         
                foreach (Vector4 coord in item.Coords)
                {
                    if (EncodeCoord(coord, distanceChannel, onehotChannel, 
                        out Vector2Int gridPos, out float distance))
                    {
                        var brush = GetBrush(coord.w);
                        foreach (Blur blur in brush)
                        {
                            Vector2Int p = gridPos + blur.Pos;
                            if (m_BlurGrid.TryRead(0, p, out float value) && blur.Value > value)
                            {
                                m_BlurGrid.Write(0, p, blur.Value);
                                // Store coord distance for each blur pixel.
                                m_BlurGrid.Write(1, p, Mathf.Max(m_BlurGrid.Read(1, p), distance));
                                // Expand area.
                                m_BlurArea.min = Vector2Int.Min(m_BlurArea.min, p);
                                m_BlurArea.max = Vector2Int.Max(m_BlurArea.max, p);
                            }
                        }
                    }
                    // else: coord already blurred at shorter distance.
                }

                bool onehot = onehotChannel != -1;
                for (int x = m_BlurArea.xMin; x <= m_BlurArea.xMax; x++)
                {
                    for (int y = m_BlurArea.yMin; y <= m_BlurArea.yMax; y++)
                    {
                        if (m_BlurGrid.TryRead(0, x, y, out float value) && value > m_BlurThreshold)
                        {
                            // Copy distance from blur grid to obs grid.
                            m_ObsGrid.Write(distanceChannel, x, y, m_BlurGrid.Read(1, x, y));

                            if (onehot)
                            {
                                m_ObsGrid.Write(onehotChannel, x, y, 1);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (Vector4 coord in item.Coords)
                {
                    EncodeCoord(coord, distanceChannel, onehotChannel,
                        out Vector2Int gridPos, out float distance);
                }
            }
        }

        private bool EncodeCoord(Vector4 coord, int distanceChannel, int onehotChannel, 
            out Vector2Int gridPos, out float distance)
        {
            gridPos = m_ObsGrid.NormalizedToGridPos(coord);
            distance = coord.z;

            // Shorter distances have higher normalized values.
            bool write = distance > m_ObsGrid.Read(distanceChannel, gridPos);

            if (write)
            {
                m_ObsGrid.Write(distanceChannel, gridPos, distance);
            }

            if (onehotChannel != -1)
            {
                m_ObsGrid.Write(onehotChannel, gridPos, 1);
            }

            return write;
        }

        private HashSet<Blur> GetBrush(float distanceRatio)
        {
            // Larger brush at shorter distance.
            return m_BlurBrushes[(int)Mathf.Min(m_BlurStrength * distanceRatio, c_MaxBlurRadius - 1)];
        }

        private void CreateBlurBrushes()
        {
            m_BlurBrushes = new HashSet<Blur>[c_MaxBlurRadius];

            for (int r = 2, n = c_MaxBlurRadius + 1; r <= n; r++)
            {
                int i = r - 2;
                m_BlurBrushes[i] = new HashSet<Blur>();

                for (int x = -r; x <= r; x++)
                {
                    for (int y = -r; y <= r; y++)
                    {
                        float value = 1 - new Vector2(x, y).magnitude / r;

                        if (value > 0)
                        {
                            m_BlurBrushes[i].Add(new Blur
                            {
                                Pos = new Vector2Int(x, y),
                                Value = value
                            });
                        }
                    }
                }
            }
        }
    }
}