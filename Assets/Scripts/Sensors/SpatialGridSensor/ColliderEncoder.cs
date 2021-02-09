using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors
{
    public enum ColliderEncodingType
    {
        DistancesOnly, OneHotAndDistances, OneHotAndShortestDistance
    }

    /// <summary>
    /// Encodes a <see cref="DetectionResult"/> in a <see cref="PixelGrid"/>.
    /// </summary>
    public class ColliderEncoder : Encoder
    {
        private readonly ColliderEncodingType m_EncodingType;
        private readonly int m_ChannelOffset;
        private readonly int m_ChannelIncr;

        public ColliderEncoder(
            PixelGrid grid, 
            ColliderEncodingType encoding, 
            IEnumerable<string> tags,
            float blurStrength, 
            float blurThreshold) 
            : base(grid, tags, blurStrength, blurThreshold)
        {
            m_EncodingType = encoding;
            m_ChannelOffset = encoding == ColliderEncodingType.OneHotAndShortestDistance ? 1 : 0;
            m_ChannelIncr = encoding == ColliderEncodingType.OneHotAndDistances ? 2 : 1;
        }

        public override void Encode(DetectionResult result)
        {
            int n = m_Grid.Shape.ChannelsPerStackLayer;
            int channel = n * m_CrntStackIndex;
            m_Grid.ClearChannels(channel, n);

            int shortestDistanceChannel = channel;
            channel += m_ChannelOffset;

            foreach (string tag in m_Tags)
            {
                var list = result.GetDetectionDataList(tag);
                foreach (var item in list)
                {
                    switch (m_EncodingType)
                    {
                        case ColliderEncodingType.DistancesOnly:
                            EncodeItem(item, channel);
                            break;

                        case ColliderEncodingType.OneHotAndShortestDistance:
                            EncodeItem(item, shortestDistanceChannel, channel);
                            break;

                        case ColliderEncodingType.OneHotAndDistances:
                            EncodeItem(item, channel, channel + 1);
                            break;
                    }
                }

                channel += m_ChannelIncr;
            }

            base.Encode(result);
        }

        private void EncodeItem(DetectionData item, int distanceChannel, int onehotChannel = -1)
        {
            if (m_ApplyBlur)
            {
                m_Blurring.NewBlur(m_Grid.NormalizedToGridRect(item.NormalizedRect));

                foreach (NormalizedPoint point in item.NormalizedPoints)
                {
                    if (EncodePoint(point, distanceChannel, onehotChannel, out Vector2Int gridPos))
                    {
                        m_Blurring.BlurPoint(gridPos, distanceChannel, point.DistanceRatio);
                    }
                    // else: coord already blurred at shorter distance.
                }

                m_Blurring.ApplyBlur(distanceChannel, onehotChannel);
            }
            else
            {
                foreach (NormalizedPoint point in item.NormalizedPoints)
                {
                    EncodePoint(point, distanceChannel, onehotChannel, out Vector2Int gridPos);
                }
            }
        }

        private bool EncodePoint(NormalizedPoint point, int distanceChannel, int onehotChannel, out Vector2Int gridPos)
        {
            gridPos = m_Grid.NormalizedToGridPos(point.Position);

            // Shorter distances have higher normalized values.
            bool write = point.Position.z > m_Grid.Read(distanceChannel, gridPos);

            if (write)
            {
                m_Grid.Write(distanceChannel, gridPos, point.Position.z);
            }

            if (onehotChannel != -1)
            {
                m_Grid.Write(onehotChannel, gridPos, 1);
            }

            return write;
        }
    }
}