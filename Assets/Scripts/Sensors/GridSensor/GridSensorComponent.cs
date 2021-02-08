using System;
using UnityEngine;
using Unity.MLAgents.Sensors;
#if (UNITY_EDITOR)
using UnityEditor;
#endif

namespace MBaske.Sensors
{
    /// <summary>
    /// Component that wraps a <see cref="GridSensor"/>.
    /// 
    /// A <see cref="PixelGrid"/> instance has to be provided either 
    /// via setting the <see cref="PixelGridProvider"/> property or by 
    /// serializing a reference to a Monobehaviour which implements
    /// <see cref="IPixelGridProvider"/>.
    /// </summary>
    public class GridSensorComponent : SensorComponent
    {
        public GridSensor Sensor { get; protected set; }

        /// <summary>
        /// Name of the generated <see cref="GridSensor"/>.
        /// </summary>
        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; UpdateSensor(); }
        }
        [HideInInspector, SerializeField]
        [Tooltip("Name of the generated GridSensor")]
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
        [Tooltip("The compression type to use for the sensor.")]
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
        [Tooltip("MonoBehaviour implementing IPixelGridProvider (optional).")]
        protected MonoBehaviour m_PixelGridProvider;


        /// <inheritdoc/>
        public override ISensor CreateSensor()
        {
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
                Sensor.PixelGrid = GetPixelGrid();
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

        public virtual GridShape GetGridShape()
        {
            return GetPixelGrid().Shape;
        }

        private void Reset()
        {
            if (TryGetComponent(out IPixelGridProvider provider))
            {
                m_PixelGridProvider = (MonoBehaviour)provider;
            }
        }
    }

#if (UNITY_EDITOR)
    [CustomEditor(typeof(GridSensorComponent))]
    [CanEditMultipleObjects]
    internal class GridSensorComponentEditor : Editor
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
                ((GridSensorComponent)target).UpdateSensor();
            }
        }
    }
#endif
}
