using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using NaughtyAttributes;
using MBaske.Sensors.Util;
using System.Collections.Generic;
using System.Collections;
using System;

namespace MBaske.Sensors.Grid 
{
    /// <summary>
    /// Abstract base class for component that wraps a single 
    /// <see cref="Grid.GridSensor"/> instance.
    /// </summary>
    public abstract class GridSensorComponentBase : SensorComponent, IDisposable
    {
        // Info.
        [SerializeField, ReadOnly]
        private string m_ObservationShape;


        #region Basic Settings

        /// <summary>
        /// Name of the generated <see cref="Grid.GridSensor"/>.
        /// Note that changing this at runtime does not affect how the Agent sorts the sensors.
        /// </summary>
        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; }
        }
        [SerializeField]
        [Foldout("Basics")]
        [Tooltip("Name of the generated GridSensor.")]
        private string m_SensorName = "GridSensor";


        /// <summary>
        /// The number of stacked observations. Enable stacking (value > 1) 
        /// if agents need to infer movement from observations.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int ObservationStacks
        {
            get { return m_ObservationStacks; }
            set { m_ObservationStacks = value; }
        }
        [SerializeField, Min(1)]
        [Foldout("Basics")]
        [Tooltip("The number of stacked observations. Enable stacking (value > 1) "
            + "if agents need to infer movement from observations.")]
        private int m_ObservationStacks = 1;


        /// <summary>
        /// The <see cref="SensorCompressionType"/> used by the sensor.
        /// </summary>
        public SensorCompressionType CompressionType
        {
            get { return m_CompressionType; }
            set { m_CompressionType = value; OnCompressionTypeChange(); }
        }
        [SerializeField]
        [OnValueChanged("OnCompressionTypeChange")]
        [Foldout("Basics")]
        [Tooltip("The compression type used by the sensor.")]
        private SensorCompressionType m_CompressionType = SensorCompressionType.PNG;

        private void OnCompressionTypeChange()
        {
            if (HasSensor)
            {
                m_GridSensor.CompressionType = m_CompressionType;
            }
        }


        /// <summary>
        /// The <see cref="Sensors.ObservationType"/> (default or goal signal) of the sensor.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public ObservationType ObservationType
        {
            get { return m_ObservationType; }
            set { m_ObservationType = value; }
        }
        [SerializeField]
        [Foldout("Basics")]
        [Tooltip("The observation type of the sensor.")]
        private ObservationType m_ObservationType = ObservationType.Default;

        #endregion



        // Debug.

        /// <summary>
        /// Optional <see cref="ChannelLabel"/> list.
        /// So far, this is utilized for debugging only.
        /// </summary>
        /// 
        public List<ChannelLabel> ChannelLabels
        {
            get { return m_ChannelLabels; }
            set { m_ChannelLabels = new List<ChannelLabel>(value); }
        }
        [SerializeField, HideInInspector]
        protected List<ChannelLabel> m_ChannelLabels;

        // Non-Editor flag for subcomponents.
        protected bool m_Debug_IsEnabled;

#if (UNITY_EDITOR)

        #region Debug Settings

        [SerializeField]
        [EnableIf("IsNotPlaying")]
        [Foldout("Debug")]
        [Label("Auto-Create Sensor")]
        [Tooltip("Whether this component should create its sensor on Awake(). Select " +
            "option to test a stand-alone sensor component not attached to an agent.")]
        private bool m_Debug_CreateSensorOnAwake;

        [SerializeField]
        [EnableIf(EConditionOperator.And, "IsNotPlaying", "m_Debug_CreateSensorOnAwake")]
        [Foldout("Debug")]
        [Label("Re-Initialize On Change")]
        [Tooltip("Whether to recreate the sensor everytime inspector settings change. " +
            "Select option to immediately see the effects of settings updates " +
            "that would otherwise not be changeable at runtime. \u2794 Only available " +
            "for auto-created sensor. Note that scene GUI edits or tag changes will " +
            "NOT trigger re-initialization and can result in errors.")]
        private bool m_Debug_CreateSensorOnValidate = true;

        [SerializeField]
        [EnableIf("m_Debug_CreateSensorOnAwake")]
        [Foldout("Debug")]
        [Label("Update Interval")]
        [Tooltip("FixedUpdate interval for auto-created sensor.")]
        [Range(1, 20)]
        private int m_Debug_FrameInterval = 1;
        private int m_Debug_FrameCount;

        [SerializeField]
        [OnValueChanged("Debug_ToggleDrawGridBuffer")]
        [Foldout("Debug")]
        [Label("Draw Grid Buffer")]
        [Tooltip("Whether to draw the grid buffer contents (runtime only). " +
            "Disable and re-enable the toggle if visualization freezes.")]
        private bool m_Debug_DrawGridBuffer;
        // Flag for tracking toggle state.
        private bool m_Debug_DrawGridBufferEnabled;
        // Inspector flag for NaughtyAttributes.
        private bool IsNotPlaying => !Application.isPlaying;

        [SerializeField]
        [ShowIf("m_Debug_DrawGridBuffer")]
        [Foldout("Debug")]
        private GridBufferDrawer m_Debug_GridBufferDrawer;
        private DebugChannelData m_Debug_ChannelData;

        #endregion

#endif

        #region Buffer and Shape

        /// <summary>
        /// The <see cref="Grid.GridBuffer"/> used for the sensor.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public GridBuffer GridBuffer
        {
            get { return m_GridBuffer; }
            set { m_GridBuffer = value; GridShape = value.GetShape(); }
        }
        private GridBuffer m_GridBuffer;

        /// <summary>
        /// The <see cref="GridBuffer.Shape"/> of the sensor's 
        /// <see cref="Grid.GridBuffer"/>.
        /// In <see cref="GridSensorComponentBase"/>, the shape is only used for 
        /// displaying the observation shape info in the inspector.
        /// However, subcomponents might update and utilize it for 
        /// generating their own <see cref="GridBuffer"/>. 
        /// </summary>
        public GridBuffer.Shape GridShape
        {
            get { return m_GridShape; }
            set { m_GridShape = value; UpdateObservationShapeInfo(); }
        }
        [SerializeField, HideInInspector]
        private GridBuffer.Shape m_GridShape = new GridBuffer.Shape(1, 20, 20);

        protected void UpdateGridChannelCount(int numChannels)
        {
            var shape = GridShape;
            shape.NumChannels = numChannels;
            GridShape = shape;
        }

        protected void UpdateGridSize(int width, int height)
        {
            var shape = GridShape;
            shape.Width = width;
            shape.Height = height;
            GridShape = shape;
        }

        private void UpdateObservationShapeInfo()
        {
            m_ObservationShape = string.Format("{0} channel{1} x {2} width x {3} height",
                    m_GridShape.NumChannels, m_GridShape.NumChannels == 1 ? "" : "s",
                    m_GridShape.Width, m_GridShape.Height);
        }

        #endregion



        /// <summary>
        /// Whether the <see cref="GridSensor"/> was created.
        /// </summary>
        public bool HasSensor
        {
            get { return m_GridSensor != null; }
        }

        /// <summary>
        /// The wrapped <see cref="Grid.GridSensor"/> instance.
        /// </summary>
        public GridSensor GridSensor
        {
            get { return m_GridSensor; }
        }
        protected GridSensor m_GridSensor;


        /// <inheritdoc/>
        public override ISensor[] CreateSensors()
        {
#if (UNITY_EDITOR)
            if (Application.isPlaying)
            {
                EditorUtil.HideBehaviorParametersEditor();

                CoroutineUtil.Stop(this, m_Debug_OnSensorCreated);
                m_Debug_OnSensorCreated = new InvokeAfterFrames(
                    this, Debug_ToggleDrawGridBuffer).Coroutine;
            }
#endif


            m_GridSensor = new GridSensor(
                m_SensorName, m_GridBuffer, m_CompressionType, m_ObservationType);

            if (m_ObservationStacks > 1)
            {
                return new ISensor[] 
                { 
                    new StackingSensor(m_GridSensor, m_ObservationStacks) 
                };
            }

            return new ISensor[] { m_GridSensor };
        }

#if (UNITY_EDITOR)

        private IEnumerator m_Debug_OnSensorCreated;

        #region Debug Methods 

        // Invoked on sensor creation and on m_Debug_DrawGridBuffer toggle change.
        private void Debug_ToggleDrawGridBuffer()
        {
            if (Debug_HasRuntimeSensor())
            {
                if (m_Debug_DrawGridBuffer != m_Debug_DrawGridBufferEnabled)
                {
                    Debug_SetDrawGridBufferEnabled(m_Debug_DrawGridBuffer);
                }
            }
        }

        private void Debug_SetDrawGridBufferEnabled(bool enabled, bool standby = false)
        {
            if (enabled)
            {
                m_Debug_ChannelData?.Dispose();
                m_Debug_ChannelData = Debug_CreateChannelData();
                m_GridSensor.UpdateEvent += m_Debug_GridBufferDrawer.OnSensorUpdate;
                ((IDebugable)m_GridSensor.Encoder)?.SetDebugEnabled(true, m_Debug_ChannelData);
                m_Debug_GridBufferDrawer.Enable(this, m_Debug_ChannelData, m_GridBuffer);
            }
            else
            {
                m_Debug_ChannelData?.Dispose();

                if (Debug_HasRuntimeSensor())
                {
                    m_GridSensor.UpdateEvent -= m_Debug_GridBufferDrawer.OnSensorUpdate;
                    ((IDebugable)m_GridSensor.Encoder)?.SetDebugEnabled(false);
                }

                if (standby)
                {
                    m_Debug_GridBufferDrawer.Standby();
                }
                else
                {
                    m_Debug_GridBufferDrawer.Disable();
                }
            }

            m_Debug_IsEnabled = enabled;
            m_Debug_DrawGridBufferEnabled = enabled;
        }

        private DebugChannelData Debug_CreateChannelData()
        {
            // Create from settings.
            if (HasSensor && m_GridSensor.AutoDetectionEnabled)
            {
                // TODO Assuming IEncoder writes grid positions to DebugChannelData.
                // Might want to parameterize this at some point.
                return DebugChannelData.FromSettings(m_GridSensor.Encoder.Settings);
            }


            // Create from labels provided via ChannelLabels property.
            if (m_ChannelLabels != null && m_ChannelLabels.Count > 0)
            {
                return DebugChannelData.FromLabels(m_ChannelLabels);
            }


            // Create fallback labels.
            int n = m_GridShape.NumChannels;
            var labels = new List<ChannelLabel>(n);

            for (int i = 0; i < n; i++)
            {
                labels.Add(new ChannelLabel(
                    "Observation " + i,
                    Color.HSVToRGB(i / (float)n, 1, 1)));
            }

            return DebugChannelData.FromLabels(labels, false);
        }


        // Standalone sensor component.

        private void Debug_CreateSensorOnAwake()
        {
            var agent = GetComponentInParent<Agent>();

            if (m_Debug_CreateSensorOnAwake)
            {
                if (agent != null)
                {
                    Debug.LogWarning("'Auto-Create Sensor' was selected, but this component is " +
                        $"attached to agent '{agent.name}'. The option is being disabled.");

                    m_Debug_CreateSensorOnAwake = false;
                }
                else
                {
                    if (!m_Debug_CreateSensorOnValidate)
                    {
                        Debug.LogWarning("Sensor might not react properly or or throw errors " +
                            "when inspector values change. You can select 'Debug > Re-Initialize " +
                            "On Change' to always refresh the sensor.");
                    }

                    CreateSensors();
                }
            }
            else if (agent == null)
            {
                Debug.LogWarning("No agent was found on this or a parent gameobject. " +
                    "You can select 'Debug > Auto-Create Sensor' to create a standalone sensor.");
            }
        }

        private void Debug_CreateSensorOnValidate()
        {
            if (Debug_HasRuntimeSensor() && m_Debug_CreateSensorOnAwake && m_Debug_CreateSensorOnValidate)
            {
                // Debug grid drawer standby during sensor refresh.
                Debug_SetDrawGridBufferEnabled(false, true);

                CreateSensors();
                m_Debug_FrameCount = 0;
            }
        }

        private void Debug_UpdateSensor()
        {
            if (m_Debug_CreateSensorOnAwake)
            {
                if (m_Debug_FrameCount % m_Debug_FrameInterval == 0)
                {
                    // We ignore the StackingSensor for debug options.
                    m_GridSensor.Update();
                }
                m_Debug_FrameCount++;
            }
        }

        private bool Debug_HasRuntimeSensor()
        {
            return Application.isPlaying && HasSensor;
        }


        private void Awake()
        {
            m_Debug_GridBufferDrawer.Disable();
            Debug_CreateSensorOnAwake();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                Debug_CreateSensorOnValidate();
            }
            else
            {
                // TODO should always catch specific changes,
                // rather than general OnValidate.
                HandleValidate();
            }
        }

        protected virtual void HandleValidate() { }

        private void FixedUpdate()
        {
            Debug_UpdateSensor();
        }

        private void OnApplicationQuit()
        {
            Debug_SetDrawGridBufferEnabled(false);
        }

        #endregion

#endif

        // Reset

        private void Reset()
        {
            HandleReset();
        }

        protected virtual void HandleReset() { }


        // Destroy / Dispose.

        private void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// Cleans up internal objects.
        /// </summary>
        public virtual void Dispose() { }
    }
}
