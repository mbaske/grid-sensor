using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace MBaske.Sensors
{
    /// <summary>
    /// Sensor class that wraps a <see cref="PixelGrid"/> instance.
    /// 
    /// Developer has to provide a PixelGrid instance and implement
    /// the logic for mapping agent observations to grid values.
    /// </summary>
    public class GridSensor : ISensor
    {
        /// <summary>
        /// Invoked on ISensor.Update()
        /// </summary>
        public event Action UpdateEvent;

        /// <summary>
        /// Invoked on ISensor.Reset()
        /// </summary>
        public event Action ResetEvent;

        /// <summary>
        /// Name of the sensor.
        /// </summary>
        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; }
        }
        protected string m_SensorName = "GridSensor";

        /// <summary>
        /// The PixelGrid used by the sensor.
        /// </summary>
        public PixelGrid PixelGrid
        {
            get { return m_PixelGrid; }
            set { m_PixelGrid = value; Allocate(); }
        }
        protected PixelGrid m_PixelGrid;

        /// <summary>
        /// The compression type used by the sensor.
        /// </summary>
        public SensorCompressionType CompressionType
        {
            get { return m_Compression; }
            set { m_Compression = value; Allocate(); }
        }
        protected SensorCompressionType m_Compression;


        protected GridShape m_Shape;
        // PNG compression.
        protected Texture2D m_Texture2D;
        protected List<byte> m_Bytes;

        /// <summary>
        /// Initializes the sensor.
        /// </summary>
        /// <param name="grid">The <see cref="PixelGrid"/> instance to wrap.</param>
        /// <param name="compression">The compression to apply to the generated image.</param>
        /// <param name="name">Name of the sensor.</param>
        public GridSensor(PixelGrid grid, SensorCompressionType compression, string name)
        {
            m_SensorName = name;
            m_PixelGrid = grid;
            m_Compression = compression;

            Allocate();
        }

        protected void Allocate()
        {
            m_Shape = m_PixelGrid.Shape;

            if (m_Compression == SensorCompressionType.PNG)
            {
                m_Texture2D = new Texture2D(m_Shape.Width, m_Shape.Height, TextureFormat.RGB24, false);
                m_Bytes = new List<byte>();
            }
        }

        /// <inheritdoc/>
        public string GetName()
        {
            return m_SensorName;
        }

        /// <inheritdoc/>
        public SensorCompressionType GetCompressionType()
        {
            return m_Compression;
        }

        /// <inheritdoc/>
        public int[] GetObservationShape()
        {
            return m_Shape.ToArray();
        }

        /// <inheritdoc/>
        public byte[] GetCompressedObservation()
        {
            m_Bytes.Clear();
            foreach (Color32[] colors in m_PixelGrid.GetColors())
            {
                m_Texture2D.SetPixels32(colors);
                m_Bytes.AddRange(m_Texture2D.EncodeToPNG());
            }

            return m_Bytes.ToArray();
        }

        /// <inheritdoc/>
        public int Write(ObservationWriter writer)
        {
            for (int c = 0; c < m_PixelGrid.Channels; c++)
            {
                for (int x = 0; x < m_PixelGrid.Width; x++)
                {
                    for (int y = 0; y < m_PixelGrid.Height; y++)
                    {
                        writer[y, x, c] = m_PixelGrid.Read(c, x, y);
                    }
                }
            }

            return m_Shape.Size;
        }

        /// <inheritdoc/>
        public virtual void Update() 
        {
            UpdateEvent?.Invoke();
        }

        /// <inheritdoc/>
        public virtual void Reset() 
        {
            m_PixelGrid.Clear();
            ResetEvent?.Invoke();
        }
    }
}
