using UnityEngine;

namespace MBaske.Sensors.Grid
{
    [HelpURL("https://github.com/mbaske/grid-sensor")]
    public class GridSensorComponent3D : GridSensorComponent
    {
        protected override GridType GridType => GridType._3D;

        [Space, SerializeField, ReadOnly]
        protected string m_SensorResolution; // info

        /// <summary>
        /// The arc angle of a single FOV grid cell in degrees. 
        /// Determines the sensor's resolution:
        /// cell size at distance = PI * 2 * distance / (360 / cell arc)
        /// </summary>
        public float CellArc
        {
            get { return m_CellArc; }
            set { m_CellArc = value; Validate(); }
        }
        [SerializeField, Range(0.25f, 9)]
        [Tooltip("The arc angle of a single FOV grid cell in degrees.")]
        protected float m_CellArc = 3;

        /// <summary>
        /// The FOV's northern latitude (up) angle in degrees.
        /// </summary>
        public float LatAngleNorth
        {
            get { return m_LatAngleNorth; }
            set { m_LatAngleNorth = value; Validate(); }
        }
        [SerializeField, Range(0, 90)]
        [Tooltip("The FOV's northern latitude (up) angle in degrees.")]
        protected float m_LatAngleNorth = 45;

        /// <summary>
        /// The FOV's southern latitude (down) angle in degrees.
        /// </summary>
        public float LatAngleSouth
        {
            get { return m_LatAngleSouth; }
            set { m_LatAngleSouth = value; Validate(); }
        }
        [SerializeField, Range(0, 90)]
        [Tooltip("The FOV's southern latitude (down) angle in degrees.")]
        protected float m_LatAngleSouth = 15;

        /// <summary>
        /// The FOV's longitude (left & right) angle in degrees.
        /// </summary>
        public float LonAngle
        {
            get { return m_LonAngle; }
            set { m_LonAngle = value; Validate(); }
        }
        [SerializeField, Range(0, 180)]
        [Tooltip("The FOV's longitude (left & right) angle in degrees.")]
        protected float m_LonAngle = 45;

        /// <summary>
        /// The minimum detection distance (near clipping).
        /// </summary>
        public float MinDistance
        {
            get { return m_MinDistance; }
            set { m_MinDistance = value; Validate(); }
        }
        [Space, SerializeField]
        [Tooltip("The minimum detection distance (near clipping).")]
        protected float m_MinDistance = 1;

        /// <summary>
        /// The maximum detection distance (far clipping).
        /// </summary>
        public float MaxDistance
        {
            get { return m_MaxDistance; }
            set { m_MaxDistance = value; Validate(); }
        }
        [SerializeField]
        [Tooltip("The maximum detection distance (far clipping).")]
        protected float m_MaxDistance = 50;

        /// <summary>
        /// Curvature applied to weighted normalization, 1 = linear.
        /// </summary>
        public DistanceNormalization DistanceNormalization
        {
            get { return m_Normalization; }
            set { m_Normalization = value; Validate(); }
        }
        [SerializeField]
        [Tooltip("How to normalize distancess. Set low value if observing distance changes "
            + "at close range is more critical to agents than what happens farther away.")]
        protected DistanceNormalization m_Normalization;

        /// <summary>
        /// The FOV's longitude/latitude rectangle.
        /// </summary>
        public Rect LonLatRect
        {
            get { return m_LonLatRect; }
        }
        [SerializeField, HideInInspector]
        protected Rect m_LonLatRect;

        /// <summary>
        /// A FOV's grid cell size at maximum detection distance.
        /// </summary>
        public float GridResolution
        {
            get { return m_GridResolution; }
        }
        [SerializeField, HideInInspector]
        protected float m_GridResolution;

        /// <summary>
        /// Quaternion array for drawing the scene GUI wireframe.
        /// </summary>
        public Quaternion[,] Wireframe
        {
            get { return m_Wireframe; }
        }
        [SerializeField, HideInInspector]
        protected Quaternion[,] m_Wireframe;


        protected override void UpdateSettings()
        {
            ClampProperties();
            UpdateGeometry();
            UpdateObservationShapeInfo();

            base.UpdateSettings();

            m_SensorResolution = string.Format("{0} meters ({1} deg at {2} meters)",
                string.Format("{0:0.00}", GridResolution),
                string.Format("{0:0.00}", CellArc),
                string.Format("{0:0.00}", MaxDistance));
        }

        protected override GameObjectDetector CreateDetector() 
            => new GameObjectDetector3D()
            {
                BufferSize = m_ColliderBufferSize,
                ClearCacheOnReset = m_ClearCacheOnReset,
                Settings = m_Detectables,
                SensorTransform = transform,
                SensorOwner = GetComponentInParent<DetectableGameObject>(),
                Constraint = new Constraint3D()
                {
                    MinRadius = m_MinDistance,
                    MaxRadius = m_MaxDistance,
                    LonLatRect = m_LonLatRect
                }
            };

        protected override GameObjectEncoder CreateEncoder()
            => new GameObjectEncoder3D()
            {
                Grid = m_PixelGrid,
                Settings = m_Detectables,
                DistanceNormalization = m_Normalization
            };


        private void ClampProperties()
        {
            m_ColliderBufferSize = Mathf.Clamp(m_ColliderBufferSize, 1, 1024);
            m_StackedObservations = Mathf.Clamp(m_StackedObservations, 1, 20);
            m_LatAngleNorth = Mathf.Clamp(m_LatAngleNorth, 0, 90);
            m_LatAngleSouth = Mathf.Clamp(m_LatAngleSouth, 0, 90);
            m_LonAngle = Mathf.Clamp(m_LonAngle, 0, 180);
            m_MinDistance = Mathf.Clamp(m_MinDistance, 0, m_MaxDistance);
            m_MaxDistance = Mathf.Max(m_MaxDistance, m_MinDistance + 1);
        }

        private void UpdateGeometry()
        {
            m_GridResolution = Mathf.PI * 2 * m_MaxDistance / (360 / m_CellArc);

            float lonMin = -m_LonAngle;
            float lonMax = m_LonAngle;
            float latMin = -m_LatAngleSouth;
            float latMax = m_LatAngleNorth;

            float lonRange = lonMax - lonMin;
            float latRange = latMax - latMin;

            int nLon = Mathf.Max(1, Mathf.CeilToInt(lonRange / m_CellArc));
            int nLat = Mathf.Max(1, Mathf.CeilToInt(latRange / m_CellArc));

            m_GridSize.x = nLon;
            m_GridSize.y = nLat;

            float lonRangePadded = nLon * m_CellArc;
            float latRangePadded = nLat * m_CellArc;

            float lonPadding = (lonRangePadded - lonRange) * 0.5f;
            float latPadding = (latRangePadded - latRange) * 0.5f;

            float lonMinPad = lonMin - lonPadding;
            float lonMinPadClamp = Mathf.Max(-180, lonMinPad);
            float lonMaxPadClamp = Mathf.Min(180, lonMax + lonPadding);

            float latMinPad = latMin - latPadding;
            float latMinPadClamp = Mathf.Max(-90, latMinPad);
            float latMaxPadClamp = Mathf.Min(90, latMax + latPadding);

            m_LonLatRect = new Rect(
                lonMinPadClamp,
                latMinPadClamp,
                lonMaxPadClamp - lonMinPadClamp,
                latMaxPadClamp - latMinPadClamp);


            // Scene GUI Wireframe.

            nLat++;
            nLon++;

            if (Wireframe == null || Wireframe.Length != nLat * nLon)
            {
                m_Wireframe = new Quaternion[nLat, nLon];
            }

            for (int iLat = 0; iLat < nLat; iLat++)
            {
                var qLat = Quaternion.AngleAxis(
                    Mathf.Clamp(latMinPad + iLat * m_CellArc, latMinPadClamp, latMaxPadClamp), Vector3.left);

                for (int iLon = 0; iLon < nLon; iLon++)
                {
                    var qLon = Quaternion.AngleAxis(
                        Mathf.Clamp(lonMinPad + iLon * m_CellArc, lonMinPadClamp, lonMaxPadClamp), Vector3.up);

                    Wireframe[iLat, iLon] = qLon * qLat;
                }
            }
        }
    }
}