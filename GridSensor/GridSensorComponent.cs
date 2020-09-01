using UnityEngine;
using Unity.MLAgents.Sensors;
#if (UNITY_EDITOR)
using UnityEditor;
#endif

namespace MBaske
{
    /// <summary>
    /// Component that wraps a <see cref="GridSensor"/>.
    /// </summary>
    [RequireComponent(typeof(IPixelGridProvider))]
    public class GridSensorComponent : SensorComponent
    {
        GridSensor m_Sensor;

        [HideInInspector, SerializeField]
        string m_SensorName = "GridSensor";

        /// <summary>
        /// Name of the generated <see cref="GridSensor"/>.
        /// Note that changing this at runtime does not affect how the Agent sorts the sensors.
        /// </summary>
        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; }
        }

        [HideInInspector, SerializeField]
        SensorCompressionType m_Compression = SensorCompressionType.None;

        /// <summary>
        /// The compression type to use for the sensor.
        /// </summary>
        public SensorCompressionType CompressionType
        {
            get { return m_Compression; }
            set { m_Compression = value; UpdateSensor(); }
        }

        /// <inheritdoc/>
        public override ISensor CreateSensor()
        {
            var grid = GetComponent<IPixelGridProvider>().GetPixelGrid();
            m_Sensor = new GridSensor(grid, m_Compression, m_SensorName);
            return m_Sensor;
        }

        /// <inheritdoc/>
        public override int[] GetObservationShape()
        {
            var grid = GetComponent<IPixelGridProvider>().GetPixelGrid(); 
            return new[] { grid.Width, grid.Height, grid.NumChannels };
        }

        /// <summary>
        /// Update fields that are safe to change on the Sensor at runtime.
        /// </summary>
        public void UpdateSensor()
        {
            if (m_Sensor != null)
            {
                m_Sensor.CompressionType = m_Compression;
            }
        }
    }

#if (UNITY_EDITOR)
    [CustomEditor(typeof(GridSensorComponent))]
    [CanEditMultipleObjects]
    internal class GridSensorComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var so = serializedObject;
            so.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_SensorName"), true);
                EditorGUILayout.PropertyField(so.FindProperty("m_Compression"), true);
            }
            EditorGUI.EndDisabledGroup();

            var requireSensorUpdate = EditorGUI.EndChangeCheck();
            so.ApplyModifiedProperties();

            if (requireSensorUpdate)
            {
                UpdateSensor();
            }
        }

        void UpdateSensor()
        {
            var sensorComponent = serializedObject.targetObject as GridSensorComponent;
            sensorComponent?.UpdateSensor();
        }
    }
#endif
}
