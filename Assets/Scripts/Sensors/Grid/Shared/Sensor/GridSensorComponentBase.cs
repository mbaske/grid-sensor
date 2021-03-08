using UnityEngine;
using Unity.MLAgents.Sensors;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Component that wraps a <see cref="GridSensor"/>.
    /// </summary>
    public class GridSensorComponentBase : SensorComponent
    {
        public GridSensor Sensor { get; private set; }

        // Info.
        [SerializeField, ReadOnly]
        protected string m_ObservationShape;

        /// <summary>
        /// Name of the generated <see cref="GridSensor"/>.
        /// </summary>
        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; Validate(); }
        }
        [SerializeField]
        [Tooltip("Name of the generated GridSensor.")]
        protected string m_SensorName = "GridSensor";

        /// <summary>
        /// The compression type to use for the sensor.
        /// </summary>
        public SensorCompressionType CompressionType
        {
            get { return m_CompressionType; }
            set { m_CompressionType = value; Validate(); }
        }
        [SerializeField]
        [Tooltip("The compression type to use for the sensor.")]
        protected SensorCompressionType m_CompressionType = SensorCompressionType.PNG;

        /// <summary>
        /// The IPixelGridProvider to use for the sensor.
        /// Optional - if not set, the sensor will look for a component
        /// implementing IPixelGridProvider, see <see cref="GetPixelGrid"/>.
        /// </summary>
        public IPixelGridProvider PixelGridProvider
        {
            get { return m_PixelGridProvider; }
            set { m_PixelGridProvider = value; Validate();  }
        }
        protected IPixelGridProvider m_PixelGridProvider;

        /// <inheritdoc/>
        public override ISensor CreateSensor()
        {
            // NOTE Validate / update settings.
            Validate();
            Sensor = new GridSensor(GetPixelGrid(), m_CompressionType, m_SensorName);
            return Sensor;
        }

        /// <inheritdoc/>
        public override int[] GetObservationShape()
        {
            return GetGridShape().ToArray();
        }

        /// <summary>
        /// Updates the sensor.
        /// </summary>
        public virtual void UpdateSensor()
        {
            if (Sensor != null)
            {
                Sensor.SensorName = m_SensorName;
                Sensor.CompressionType = m_CompressionType;
                Sensor.Grid = GetPixelGrid();
            }
        }

        public virtual PixelGrid GetPixelGrid()
        {
            m_PixelGridProvider ??= GetComponentInChildren<IPixelGridProvider>();
            
            if (m_PixelGridProvider != null)
            {
                return m_PixelGridProvider.GetPixelGrid();
            }

            throw new MissingReferenceException("PixelGridProvider not available.");
        }

        public virtual GridShape GetGridShape()
        {
            return GetPixelGrid().Shape;
        }

        protected void UpdateObservationShapeInfo()
        {
            m_ObservationShape = GetGridShape().ToString();
        }

        protected virtual void UpdateSettings() { }

        public void Validate()
        {
            UpdateSettings();
            UpdateSensor();
        }

        private void OnValidate()
        {
            Validate();
        }
    }
}
