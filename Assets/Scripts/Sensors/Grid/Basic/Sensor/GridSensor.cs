using Unity.MLAgents.Sensors;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Sensor that generates visual observations 
    /// from <see cref="GridBuffer"/> contents.
    /// </summary>
    public class GridSensor : ISensor, IDisposable
    {
        /// <summary>
        /// Invoked on <see cref="ISensor.Update"/>.
        /// </summary>
        public event Action UpdateEvent;

        /// <summary>
        /// Invoked on <see cref="ISensor.Reset"/>.
        /// </summary>
        public event Action ResetEvent;

        /// <summary>
        /// Optional <see cref="IDetector"/> to use for the sensor.
        /// </summary>
        public IDetector Detector { get; private set; }

        /// <summary>
        /// Optional <see cref="IEncoder"/> to use for the sensor.
        /// </summary>
        public IEncoder Encoder { get; private set; }

        /// <summary>
        /// Whether the sensor utilizes <see cref="Detector"/> and <see cref="Encoder"/>.
        /// </summary>
        public bool AutoDetectionEnabled { get; private set; }

        /// <summary>
        /// The <see cref="SensorCompressionType"/> type used by the sensor.
        /// </summary>
        public SensorCompressionType CompressionType
        {
            get { return m_CompressionType; }
            set { m_CompressionType = value; HandleCompressionType(); }
        }
        private SensorCompressionType m_CompressionType;


        private readonly string m_Name;
        private readonly GridBuffer m_GridBuffer;
        private readonly ObservationSpec m_ObservationSpec;
        // PNG compression.
        private Texture2D m_PerceptionTexture;
        private List<byte> m_CompressedObs;

        /// <summary>
        /// Creates a <see cref="GridSensor"/> instance.
        /// </summary>
        /// <param name="buffer">The <see cref="GridBuffer"/> instance to wrap</param>
        /// <param name="compressionType">The <see cref="SensorCompressionType"/> 
        /// to apply to the generated image</param>
        /// <param name="observationType">The <see cref="ObservationType"/> 
        /// (default or goal signal) of the sensor</param>
        /// <param name="name">Name of the sensor</param>
        public GridSensor(
            string name,
            GridBuffer buffer,
            SensorCompressionType compressionType,
            ObservationType observationType)
        {
            m_Name = name;

            buffer.GetShape().Validate();
            m_GridBuffer = buffer;

            m_CompressionType = compressionType;
            HandleCompressionType();

            m_ObservationSpec = ObservationSpec.Visual(
                m_GridBuffer.Height, m_GridBuffer.Width, m_GridBuffer.NumChannels, observationType);
        }

        /// <summary>
        /// Add <see cref="Detector"/> and <see cref="Encoder"/> for auto-detection on <see cref="Update"/>.
        /// </summary>
        /// <param name="detector"><see cref="IDetector"/></param>
        /// <param name="encoder"><see cref="IEncoder"/></param>
        public void EnableAutoDetection(IDetector detector, IEncoder encoder)
        {
            Detector = detector;
            Encoder = encoder;
            AutoDetectionEnabled = true;
        }

        protected void HandleCompressionType()
        {
            DestroyTexture();

            if (m_CompressionType == SensorCompressionType.PNG)
            {
                m_PerceptionTexture = new Texture2D(
                    m_GridBuffer.Width, m_GridBuffer.Height, TextureFormat.RGB24, false);
                m_CompressedObs = new List<byte>(
                    m_GridBuffer.Width * m_GridBuffer.Height * m_GridBuffer.NumChannels);
            }
        }

        /// <inheritdoc/>
        public string GetName()
        {
            return m_Name;
        }

        /// <inheritdoc/>
        public ObservationSpec GetObservationSpec()
        {
            return m_ObservationSpec;
        }

        /// <inheritdoc/>
        public CompressionSpec GetCompressionSpec()
        {
            return new CompressionSpec(CompressionType);
        }

        /// <inheritdoc/>
        public byte[] GetCompressedObservation()
        {
            m_CompressedObs.Clear();

            var colors = m_GridBuffer.GetLayerColors();
            for (int i = 0, n = colors.Length; i < n; i++)
            {
                m_PerceptionTexture.SetPixels32(colors[i]);
                m_CompressedObs.AddRange(m_PerceptionTexture.EncodeToPNG());
            }

            return m_CompressedObs.ToArray();
        }

        /// <inheritdoc/>
        public int Write(ObservationWriter writer)
        {
            int numWritten = 0;
            int w = m_GridBuffer.Width;
            int h = m_GridBuffer.Height;
            int n = m_GridBuffer.NumChannels;

            for (int c = 0; c < n; c++)
            {
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        writer[y, x, c] = m_GridBuffer.Read(c, x, y);
                        numWritten++;
                    }
                }
            }

            return numWritten;
        }

        /// <inheritdoc/>
        public virtual void Update() 
        {
            if (AutoDetectionEnabled)
            {
                Detector.OnSensorUpdate();
                Encoder.Encode(Detector.Result);
            }

            UpdateEvent?.Invoke();
        }

        /// <inheritdoc/>
        public virtual void Reset() 
        {
            Detector?.OnSensorReset();
            ResetEvent?.Invoke();
        }

        /// <summary>
        /// Cleans up internal objects.
        /// </summary>
        public void Dispose()
        {
            DestroyTexture();
        }

        private void DestroyTexture()
        {
            if (m_PerceptionTexture is object)
            {
                if (Application.isEditor)
                {
                    // Edit Mode tests complain if we use Destroy()
                    UnityEngine.Object.DestroyImmediate(m_PerceptionTexture);
                }
                else
                {
                    UnityEngine.Object.Destroy(m_PerceptionTexture);
                }

                m_PerceptionTexture = null;
            }
        }
    }
}
