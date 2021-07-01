using UnityEngine;
using NaughtyAttributes;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Sensor component for detecting gameobjects in 3D space.
    /// </summary>
    [HelpURL("https://github.com/mbaske/grid-sensor")]
    [RequireComponent(typeof(GridEditorHelper3D))]
    public class GridSensorComponent3D : GridSensorComponentBaseGO
    {
        protected override DetectorSpaceType DetectorSpaceType => DetectorSpaceType.Sphere;

        // Info.
        [SerializeField]
        [ReadOnly]
        [Foldout("Geometry")]
        private string m_SensorResolution;

        private void UpdateResolutionInfo()
        {
            m_SensorResolution = string.Format("{0} meters ({1} deg at {2} meters)",
                string.Format("{0:0.00}", GridResolution),
                string.Format("{0:0.00}", CellArc),
                string.Format("{0:0.00}", MaxDistance));
        }


        #region Geometry Settings

        /// <summary>
        /// The arc angle of a single FOV grid cell in degrees. 
        /// Determines the sensor's resolution:
        /// Cell size at distance = PI * 2 * distance / (360 / cell arc)
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public float CellArc
        {
            get { return m_CellArc; } 
            set { m_CellArc = value; OnFOVChange(); }
        }
        [SerializeField]
        [OnValueChanged("OnFOVChange")]
        [Foldout("Geometry")]
        [Tooltip("The arc angle of a single FOV grid cell in degrees.")]
        [Range(0.25f, 9)] private float m_CellArc = 3;

        /// <summary>
        /// The FOV's northern latitude (up) angle in degrees.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public float LatAngleNorth
        {
            get { return m_LatAngleNorth; }
            set { m_LatAngleNorth = value; OnFOVChange(); }
        }
        [SerializeField]
        [OnValueChanged("OnFOVChange")]
        [Foldout("Geometry")]
        [Tooltip("The FOV's northern latitude (up) angle in degrees.")]
        [Range(0, 90)] private float m_LatAngleNorth = 45;

        /// <summary>
        /// The FOV's southern latitude (down) angle in degrees.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public float LatAngleSouth
        {
            get { return m_LatAngleSouth; }
            set { m_LatAngleSouth = value; OnFOVChange(); }
        }
        [SerializeField]
        [OnValueChanged("OnFOVChange")]
        [Foldout("Geometry")]
        [Tooltip("The FOV's southern latitude (down) angle in degrees.")]
        [Range(0, 90)] private float m_LatAngleSouth = 15;

        /// <summary>
        /// The FOV's longitude (left & right) angle in degrees.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public float LonAngle
        {
            get { return m_LonAngle; }
            set { m_LonAngle = value; OnFOVChange(); }
        }
        [SerializeField]
        [OnValueChanged("OnFOVChange")]
        [Foldout("Geometry")]
        [Tooltip("The FOV's longitude (left & right) angle in degrees.")]
        [Range(0, 180)] private float m_LonAngle = 45;

        private void OnFOVChange()
        {
            m_LatAngleNorth = Mathf.Clamp(m_LatAngleNorth, 0, 90);
            m_LatAngleSouth = Mathf.Clamp(m_LatAngleSouth, 0, 90);
            m_LonAngle = Mathf.Clamp(m_LonAngle, 0, 180);

            UpdateGeometry();
            // Saving for Scene GUI Undo.
            SaveState(OnFOVChange, "Grid Sensor FOV Change");
        }


        /// <summary>
        /// The minimum detection distance (near clipping).
        /// </summary>
        public float MinDistance
        {
            get { return m_MinDistance; }
            set { m_MinDistance = value; OnDistanceChange(); }
        }
        [Space, SerializeField]
        [OnValueChanged("OnDistanceChange")]
        [Foldout("Geometry")]
        [Tooltip("The minimum detection distance (near clipping).")]
        private float m_MinDistance = 1;

        /// <summary>
        /// The maximum detection distance (far clipping).
        /// </summary>
        public float MaxDistance
        {
            get { return m_MaxDistance; }
            set { m_MaxDistance = value; OnDistanceChange(); }
        }
        [SerializeField]
        [OnValueChanged("OnDistanceChange")]
        [Foldout("Geometry")]
        [Tooltip("The maximum detection distance (far clipping).")]
        private float m_MaxDistance = 50;

        /// <summary>
        /// Curvature applied to weighted normalization, 1 = linear.
        /// </summary>
        public DistanceNormalization DistanceNormalization
        {
            get { return m_DistanceNormalization; }
            set { m_DistanceNormalization = value; OnDistanceChange(); }
        }
        [SerializeField]
        [OnValueChanged("OnDistanceChange")]
        [Foldout("Geometry")]
        [Tooltip("How to normalize distancess. Set low value if observing distance changes "
            + "at close range is more critical to agents than what happens farther away.")]
        private DistanceNormalization m_DistanceNormalization;

        private void OnDistanceChange()
        {
            m_MinDistance = Mathf.Clamp(m_MinDistance, 0, m_MaxDistance);
            m_MaxDistance = Mathf.Max(m_MaxDistance, m_MinDistance + 1);

            UpdateGeometry();
            // Saving for Scene GUI Undo.
            SaveState(OnDistanceChange, "Grid Sensor Distance Change");

            if (HasSensor)
            {
                ((GameObjectDetector3D)m_GridSensor.Detector)
                    .Constraint = (DetectionConstraint3D)CreateConstraint();
                ((Encoder)m_GridSensor.Encoder)
                    .DistanceNormalization = m_DistanceNormalization;
            }
        }

        #endregion


        /// <summary>
        /// The FOV's longitude/latitude rectangle.
        /// </summary>
        public Rect LonLatRect
        {
            get { return m_LonLatRect; }
        }
        [SerializeField, HideInInspector]
        private Rect m_LonLatRect;

        /// <summary>
        /// Grid cell world size at maximum detection distance.
        /// </summary>
        public float GridResolution
        {
            get { return m_GridResolution; }
        }
        [SerializeField, HideInInspector]
        private float m_GridResolution;


        /// <summary>
        /// Invoked by <see cref="GridEditorHelper3D"/>.
        /// </summary>
        public override void OnEditorInit()
        {
            base.OnEditorInit();

            // Will update grid shape info.
            UpdateGeometry();
   
            SaveState(OnFOVChange, "Grid Sensor FOV Change");
            SaveState(OnDistanceChange, "Grid Sensor Distance Change");
        }

        protected override GameObjectDetector CreateDetector()
        {
            return new GameObjectDetector3D()
            {
                Result = new DetectionResult(
                    m_GameObjectSettingsMeta.DetectableTags, 
                    ColliderBufferSize),

                Settings = m_GameObjectSettingsMeta,
                ColliderBufferSize = ColliderBufferSize,
                ClearCacheOnReset = ClearCacheOnReset,
                SensorTransform = transform,
                SensorOwner = GetComponentInParent<DetectableGameObject>(),
                Constraint = (DetectionConstraint3D)CreateConstraint()
            };
        }

        protected override Encoder CreateEncoder()
        {
            return new Encoder()
            {
                Settings = m_GameObjectSettingsMeta,
                GridBuffer = GridBuffer,
                DistanceNormalization = DistanceNormalization
            };
        }

        protected override DetectionConstraint CreateConstraint()
        {
            return new DetectionConstraint3D()
            {
                MinRadius = MinDistance,
                MaxRadius = MaxDistance,
                LonLatRect = LonLatRect
            };
        }

        private void UpdateGeometry()
        {
            m_GridResolution = GeomUtil3D.GetGridResolution(m_CellArc, m_MaxDistance);
            m_LonLatRect = GeomUtil3D.GetLonLatRect(m_CellArc, 
                m_LonAngle, m_LatAngleSouth, m_LatAngleNorth, 
                out int nLon, out int nLat);

            UpdateGridSize(nLon, nLat);
            UpdateResolutionInfo();
        }
    }
}