using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using System.Linq;

namespace MBaske.Sensors
{
    [HelpURL("https://github.com/mbaske/grid-sensor")]
    public class SpatialGridSensorComponent : GridSensorComponent, IPixelGridProvider
    {
        /// <summary>
        /// The maximum number of colliders the sensor can detect at once.
        /// </summary>
        public int BufferSize
        {
            get { return m_BufferSize; }
            set { m_BufferSize = value; UpdateSensor(); }
        }
        [HideInInspector, SerializeField]
        [Tooltip("The maximum number of colliders the sensor can detect at once.")]
        protected int m_BufferSize = 64;

        /// <summary>
        /// The number of stacked observations. Enable stacking (value > 1) 
        /// if the agent needs to infer movement from observations.
        /// </summary>
        public int ObservationStackSize
        {
            get { return m_ObservationStackSize; }
            set { m_ObservationStackSize = value; UpdateSensor(); }
        }
        [HideInInspector, SerializeField]
        [Tooltip("The number of stacked observations. Enable stacking (value > 1) if the agent needs to infer movement from observations.")]
        protected int m_ObservationStackSize = 1;

        /// <summary>
        /// The layers used for detecting colliders.
        /// </summary>
        public int[] Layers
        {
            get { return m_Layers; }
            set { m_Layers = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("The layers used for detecting colliders.")]
        protected int[] m_Layers = new int[] { 0 };
        public int LayerMask => m_Layers.Distinct().Select(l => 1 << l).Sum();

        /// <summary>
        /// The tags used for filtering detected colliders.
        /// </summary>
        public string[] Tags
        {
            get { return m_Tags; }
            set { m_Tags = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("The tags used for filtering detected colliders.")]
        protected string[] m_Tags = new string[] { "Untagged" };
        public IEnumerable<string> DistinctTags => m_Tags.Distinct();

        /// <summary>
        /// How to encode a <see cref="DetectionResult"/> in a <see cref="PixelGrid"/>.
        /// Together with the number of tags, this setting determines how many observation 
        /// channels will be used.
        /// - DistancesOnly: One channel per tag (distance).
        /// - OneHotAndDistances: Two channels per tag (one-hot & distance).
        /// - OneHotAndShortestDistance: One channel per tag (one-hot) plus a 
        ///   single channel for the shortest measured distance, regardless of 
        ///   the tag.
        /// </summary>
        public ColliderEncodingType ChannelEncoding
        {
            get { return m_ChannelEncoding; }
            set { m_ChannelEncoding = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("How to encode detection results as grid values.")]
        protected ColliderEncodingType m_ChannelEncoding;

        /// <summary>
        /// What to detect about a collider.
        /// - Position: The position of the collider's transform.
        /// - ClosestPoint: Point on the collider that's closest to the sensor.
        /// - Shape: A set of points roughly representing the collider's shape.
        ///   (including closet point).
        /// </summary>
        public ColliderDetectionType DetectionType
        {
            get { return m_DetectionType; }
            set { m_DetectionType = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("What to detect about a collider.")]
        protected ColliderDetectionType m_DetectionType;

        /// <summary>
        /// The distance between points that represent a collider's shape.
        /// </summary>
        public float ScanResolution
        {
            get { return m_ScanResolution; }
            set { m_ScanResolution = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("The distance between points that represent a collider's shape.")]
        protected float m_ScanResolution = 1;

        /// <summary>
        /// The maximum axis-aligned point distance from the collider's center.
        /// </summary>
        public float ScanExtent
        {
            get { return m_ScanExtent; }
            set { m_ScanExtent = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("The maximum axis-aligned point distance from the collider's center.")]
        protected float m_ScanExtent = 5;

        /// <summary>
        /// Whether to clear the collider shape cache on sensor reset (end of episode). 
        /// Should be disabled if colliders don't change from one episode to the next.
        /// </summary>
        public bool ClearCacheOnReset
        {
            get { return m_ClearCacheOnReset; }
            set { m_ClearCacheOnReset = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("Whether to clear the collider shape cache on sensor reset (end of episode).")]
        protected bool m_ClearCacheOnReset;

        /// <summary>
        /// How strongly grid pixels should be blurred. This value is multiplied with 
        /// the points' inverse distances, so that closer points are blurred more strongly. 
        /// Blur Strength = 0 disables blurring.
        /// </summary>
        public float BlurStrength
        {
            get { return m_BlurStrength; }
            set { m_BlurStrength = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("How strongly grid pixels should be blurred. 0 disables blurring.")]
        protected float m_BlurStrength;

        /// <summary>
        /// A cutoff value controlling how much of a blurred area is drawn onto the grid.
        /// </summary>
        public float BlurThreshold
        {
            get { return m_BlurThreshold; }
            set { m_BlurThreshold = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("A cutoff value controlling how much of a blurred area is drawn onto the grid.")]
        protected float m_BlurThreshold;

        /// <summary>
        /// The arc angle of a single FOV grid cell in degrees. 
        /// Determines the sensor's resolution:
        /// cell size at distance = PI * 2 * distance / (360 / cell arc)
        /// </summary>
        public float CellArc
        {
            get { return m_CellArc; }
            set { m_CellArc = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("The arc angle of a single FOV grid cell in degrees. ")]
        protected float m_CellArc = 5f;

        /// <summary>
        /// The FOV's northern latitude (up) angle in degrees.
        /// </summary>
        public float LatAngleNorth
        {
            get { return m_LatAngleNorth; }
            set { m_LatAngleNorth = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("The FOV's northern latitude (up) angle in degrees.")]
        protected float m_LatAngleNorth = 45;

        /// <summary>
        /// The FOV's southern latitude (down) angle in degrees.
        /// </summary>
        public float LatAngleSouth
        {
            get { return m_LatAngleSouth; }
            set { m_LatAngleSouth = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("The FOV's southern latitude (down) angle in degrees.")]
        protected float m_LatAngleSouth = 15;

        /// <summary>
        /// The FOV's longitude (left & right) angle in degrees.
        /// </summary>
        public float LonAngle
        {
            get { return m_LonAngle; }
            set { m_LonAngle = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("The FOV's longitude (left & right) angle in degrees.")]
        protected float m_LonAngle = 45;

        /// <summary>
        /// The minimum detection distance (near clipping).
        /// </summary>
        public float MinDistance
        {
            get { return m_MinDistance; }
            set { m_MinDistance = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("The minimum detection distance (near clipping).")]
        protected float m_MinDistance = 1f;

        /// <summary>
        /// The maximum detection distance (far clipping).
        /// </summary>
        public float MaxDistance
        {
            get { return m_MaxDistance; }
            set { m_MaxDistance = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("The maximum detection distance (far clipping).")]
        protected float m_MaxDistance = 10f;

        /// <summary>
        /// How to normalize distances values, `Linear` or `Weighted`. 
        /// Use `Weighted` if observing distance changes at close range 
        /// is more critical than further away.
        /// </summary>
        public DistanceNormalization DistanceNormalization
        {
            get { return m_DistanceNormalization; }
            set { m_DistanceNormalization = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("How to normalize distances values. Use Weighted if observing distance changes at close range is more critical to the agent than what happens farther away.")]
        protected DistanceNormalization m_DistanceNormalization;

        /// <summary>
        /// Curvature strength applied to Weighted normalization.
        /// </summary>
        public float NormalizationWeight
        {
            get { return m_NormalizationWeight; }
            set { m_NormalizationWeight = value; UpdateSensor(); }
        }
        [SerializeField, HideInInspector]
        [Tooltip("Curvature strength applied to Weighted normalization.")]
        protected float m_NormalizationWeight = 0.5f;

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
        /// The dimensions of the observed grid.
        /// </summary>
        public Vector2Int GridDimensions
        {
            get { return m_GridDimensions; }
        }
        [SerializeField, HideInInspector]
        protected Vector2Int m_GridDimensions;

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



        // TODO how do we catch resets in custom editor?
        [HideInInspector]
        public bool ResetFlag;

        private void Reset()
        {
            ResetFlag = true;
        }

        private void Awake()
        {
            UpdateGeometry();
        }

        protected PixelGrid NewGrid()
            => new PixelGrid(GetGridShape());

        // Unlike the base GridSensorComponent, 
        // this component generates its own PixelGrid
        // and therefore implements IPixelGridProvider.
        protected PixelGrid m_Grid;

        protected Detector NewDetector() 
            => new ColliderDetector(
            transform,
            m_BufferSize,
            LayerMask,
            DistinctTags,
            m_LonLatRect,
            m_MinDistance,
            m_MaxDistance,
            m_NormalizationWeight,
            m_DistanceNormalization,
            m_DetectionType,
            m_ScanResolution,
            m_ScanExtent,
            m_ClearCacheOnReset);

        protected Encoder NewEncoder() 
            => new ColliderEncoder(
            GetPixelGrid(),
            m_ChannelEncoding,
            DistinctTags,
            m_BlurStrength,
            m_BlurThreshold);


        public override ISensor CreateSensor()
        {
            base.CreateSensor();
            Sensor.Detector = NewDetector();
            Sensor.Encoder = NewEncoder();
            return Sensor;
        }

        public override void UpdateSensor()
        {
            ClampProperties();
            UpdateGeometry();

            if (Sensor != null)
            {
                m_Grid = NewGrid();
                base.UpdateSensor();
                Sensor.Detector = NewDetector();
                Sensor.Encoder = NewEncoder();
            }
        }

        public override PixelGrid GetPixelGrid()
        {
            m_Grid ??= NewGrid();
            return m_Grid;
        }

        public override GridShape GetGridShape()
        {
            int channels = DistinctTags.Count();

            switch (m_ChannelEncoding)
            {
                case ColliderEncodingType.OneHotAndShortestDistance:
                    channels++;
                    break;

                case ColliderEncodingType.OneHotAndDistances:
                    channels *= 2;
                    break;
            }

            return new GridShape(m_ObservationStackSize, channels, m_GridDimensions);
        }


        public bool HasDetectionResult(out DetectionResult result)
        {
            if (Sensor != null)
            {
                result = Sensor.Detector.Result;
                return true;
            }

            result = null;
            return false;
        }

        public bool HasDetectionStats(out string stats)
        {
            if (Sensor != null)
            {
                stats = Sensor.Detector.Stats();
                return true;
            }

            stats = "";
            return false;
        }


        protected void ClampProperties()
        {
            m_BufferSize = Mathf.Clamp(m_BufferSize, 1, 1024);
            m_ObservationStackSize = Mathf.Clamp(m_ObservationStackSize, 1, 20);
            m_LatAngleNorth = Mathf.Clamp(m_LatAngleNorth, 0, 90);
            m_LatAngleSouth = Mathf.Clamp(m_LatAngleSouth, 0, 90);
            m_LonAngle = Mathf.Clamp(m_LonAngle, 0, 180);
            m_MinDistance = Mathf.Clamp(m_MinDistance, 0, m_MaxDistance);
            m_MaxDistance = Mathf.Max(m_MaxDistance, m_MinDistance + 1);
        }

        protected void UpdateGeometry()
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

            m_GridDimensions.x = nLon;
            m_GridDimensions.y = nLat;

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