using UnityEngine;
using Unity.MLAgents.Sensors;

namespace MBaske.Sensors.Util
{
    /// <summary>
    /// Creates and updates a <see cref="GridSensor"/>.
    /// Can be used for testing a standalone sensor component.
    /// </summary>
    [RequireComponent(typeof(GridSensorComponent))]
    public class DebugStandaloneSensor : MonoBehaviour
    {
        private ISensor m_Sensor;

        private void Awake()
        {
            m_Sensor = GetComponent<GridSensorComponent>().CreateSensor();
        }

        private void Update()
        {
            m_Sensor.Update();
        }
    }
}