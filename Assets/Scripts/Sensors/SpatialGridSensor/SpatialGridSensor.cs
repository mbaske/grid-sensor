using Unity.MLAgents.Sensors;
using UnityEngine;

namespace MBaske.Sensors
{
    public class SpatialGridSensor : GridSensor
    {
        /// <summary>
        /// The detector to use for the sensor.
        /// </summary>
        public IColliderDetector Detector
        {
            get { return m_Detector; }
            set { m_Detector = value; }
        }
        protected IColliderDetector m_Detector;

        /// <summary>
        /// The encoder to use for the sensor.
        /// </summary>
        public IGridEncoder Encoder
        {
            get { return m_Encoder; }
            set { m_Encoder = value; }
        }
        protected IGridEncoder m_Encoder;


        public SpatialGridSensor(IColliderDetector detector, IGridEncoder encoder, 
            PixelGrid grid, SensorCompressionType compression, string name) 
            : base(grid, compression, name)
        {
            m_Detector = detector;
            m_Encoder = encoder;
        }

        /// <inheritdoc/>
        public override void Update()
        {
            m_Encoder.Encode(m_Detector.Update());
            base.Update();
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            m_Detector.Reset();
            m_Encoder.Reset();
            base.Reset();
        }
    }
}