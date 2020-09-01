using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections.Generic;

namespace MBaske
{
    /// <summary>
    /// Sensor class that wraps a <see cref="PixelGrid"/> instance.
    /// Developer has to provide a <see cref="PixelGrid"/> instance and 
    /// implement the logic for mapping agent observations to grid values.
    /// </summary>
    public class GridSensor : ISensor
    {
        PixelGrid m_Grid;
        string m_Name;
        int[] m_Shape;
        SensorCompressionType m_CompressionType;
        Texture2D m_Texture2D;
        List<byte> m_Bytes;

        /// <summary>
        /// Initializes the sensor.
        /// </summary>
        /// <param name="grid">The <see cref="PixelGrid"/> instance to wrap.</param>
        /// <param name="compression">The compression to apply to the generated image.</param>
        /// <param name="name">Name of the sensor.</param>
        public GridSensor(PixelGrid grid, SensorCompressionType compression, string name)
        {
            m_Grid = grid;
            m_CompressionType = compression;
            m_Name = name;
            m_Shape = new[] { m_Grid.Height, m_Grid.Width, m_Grid.NumChannels };
            m_Texture2D = new Texture2D(m_Grid.Width, m_Grid.Height, TextureFormat.RGB24, false);
            m_Bytes = new List<byte>(m_Grid.Width * m_Grid.Height * m_Grid.NumLayers);
        }

        /// <summary>
        /// The compression type used by the sensor.
        /// </summary>
        public SensorCompressionType CompressionType
        {
            get { return m_CompressionType; }
            set { m_CompressionType = value; }
        }

        /// <inheritdoc/>
        public SensorCompressionType GetCompressionType()
        {
            return m_CompressionType;
        }

        /// <inheritdoc/>
        public string GetName()
        {
            return m_Name;
        }

        /// <inheritdoc/>
        public int[] GetObservationShape()
        {
            return m_Shape;
        }

        /// <inheritdoc/>
        public byte[] GetCompressedObservation()
        {
            m_Bytes.Clear();
            foreach (Color32[] colors in m_Grid.GetColors())
            {
                m_Texture2D.SetPixels32(colors);
                m_Bytes.AddRange(m_Texture2D.EncodeToPNG());
            }
            return m_Bytes.ToArray();
        }

        /// <inheritdoc/>
        public int Write(ObservationWriter writer)
        {
            for (int c = 0; c < m_Grid.NumChannels; c++)
            {
                for (int x = 0; x < m_Grid.Width; x++)
                {
                    for (int y = 0; y < m_Grid.Height; y++)
                    {
                        writer[y, x, c] = m_Grid.Read(c, x, y);
                    }
                }
            }
            return m_Grid.NumValues;
        }

        /// <inheritdoc/>
        public void Update() { }

        /// <inheritdoc/>
        public void Reset() { }
    }
}
