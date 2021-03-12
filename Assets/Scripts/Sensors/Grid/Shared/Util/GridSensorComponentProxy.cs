using UnityEngine;
using Unity.MLAgents.Sensors;

namespace MBaske.Sensors.Grid.Util
{
    /// <summary>
    /// Stand-in for <see cref="GridSensorComponentBase"/>.
    /// Use if sensor transform must be separate from agent transform.
    /// </summary>
    public class GridSensorComponentProxy : SensorComponent
    {
        [SerializeField]
        protected GridSensorComponentBase m_GridSensorComponent;

        public GridSensor Sensor 
            => m_GridSensorComponent.Sensor;

        public string SensorName 
            => m_GridSensorComponent.SensorName;

        public SensorCompressionType CompressionType 
            => m_GridSensorComponent.CompressionType;

        public override ISensor CreateSensor()
            => m_GridSensorComponent.CreateSensor();

        public override int[] GetObservationShape()
            => m_GridSensorComponent.GetObservationShape();
    }
}
