using System;
using UnityEngine;
using Unity.MLAgents.Sensors;
#if (UNITY_EDITOR)
using UnityEditor;
#endif

namespace MLGridSensor
{
    /// <summary>
    /// Component that wraps a <see cref="GridSensor"/>.
    /// </summary>
    public class GridSensorComponent : SensorComponent
    {
        /// <summary>
        /// Name of the generated <see cref="GridSensor"/>.
        /// </summary>
        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; UpdateSensor(); }
        }
        [HideInInspector, SerializeField]
        protected string m_SensorName = "GridSensor";

        /// <summary>
        /// The compression type to use for the sensor.
        /// </summary>
        public SensorCompressionType CompressionType
        {
            get { return m_CompressionType; }
            set { m_CompressionType = value; UpdateSensor(); }
        }
        [HideInInspector, SerializeField]
        protected SensorCompressionType m_CompressionType = SensorCompressionType.PNG;

        /// <summary>
        /// The IPixelGridProvider to use for the sensor.
        /// Optional - if not set, the serialized MonoBehaviour will be used.
        /// </summary>
        public IPixelGridProvider PixelGridProvider
        {
            get { return m_IPixelGridProvider; }
            set { m_IPixelGridProvider = value; UpdateSensor();  }
        }
        protected IPixelGridProvider m_IPixelGridProvider;

        // TODO Can't serialize an interface. 
        // Assuming a MonoBehaviour is implementing IPixelGridProvider.
        [HideInInspector, SerializeField]
        protected MonoBehaviour m_PixelGridProvider;

        protected GridSensor m_Sensor;

        /// <inheritdoc/>
        public override ISensor CreateSensor()
        {
            m_Sensor ??= new GridSensor(GetPixelGrid(), m_CompressionType, m_SensorName);
            return m_Sensor;
        }

        /// <inheritdoc/>
        public override int[] GetObservationShape()
        {
            return GetPixelGrid().Shape.ToArray();
        }

        /// <summary>
        /// Updates the sensor.
        /// </summary>
        public virtual void UpdateSensor()
        {
            if (m_Sensor != null)
            {
                m_Sensor.SensorName = m_SensorName;
                m_Sensor.CompressionType = m_CompressionType;
                m_Sensor.PixelGrid = GetPixelGrid();
            }
        }

        public virtual PixelGrid GetPixelGrid()
        {
            if (m_IPixelGridProvider != null)
            {
                // Takes precedence over serialized MonoBehaviour.
                return m_IPixelGridProvider.GetPixelGrid();
            }

            if (m_PixelGridProvider == null)
            {
                throw new MissingFieldException("Missing PixelGridProvider");
            }
            else if (!(m_PixelGridProvider is IPixelGridProvider))
            {
                throw new NotImplementedException("Serialized MonoBehaviour doesn't implement IPixelGridProvider");
            }

            return ((IPixelGridProvider)m_PixelGridProvider).GetPixelGrid();
        }

        private void Reset()
        {
            var provider = GetComponent<IPixelGridProvider>();
            if (provider != null)
            {
                m_PixelGridProvider = (MonoBehaviour)provider;
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
            {
                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                {
                    EditorGUILayout.PropertyField(so.FindProperty("m_SensorName"), true);
                    EditorGUILayout.PropertyField(so.FindProperty("m_Compression"), true);
                    EditorGUILayout.PropertyField(so.FindProperty("m_PixelGridProvider"), true);
                }
                EditorGUI.EndDisabledGroup();
            }
            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                var comp = serializedObject.targetObject as GridSensorComponent;
                comp.UpdateSensor();
            }
        }
    }
#endif
}
