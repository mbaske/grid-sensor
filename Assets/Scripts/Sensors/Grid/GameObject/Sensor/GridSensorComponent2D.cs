using UnityEngine;

namespace MBaske.Sensors.Grid
{
    [HelpURL("https://github.com/mbaske/grid-sensor")]
    public class GridSensorComponent2D : GridSensorComponent
    {
        protected override GridType GridType => GridType._2D;

        /// <summary>
        /// Whether to Y-rotate detection bounds together with the sensor transform.
        /// </summary>
        public bool Rotate
        {
            get { return m_Rotate; }
            set { m_Rotate = value; }
        }
        [SerializeField, Tooltip("Whether to Y-rotate detection bounds with the sensor transform." +
            " If disabled, bounds are always aligned with world forward axis.")]
        private bool m_Rotate = true;

        public Quaternion Rotation
        {
            get { return m_Rotate
                    ? Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) 
                    : m_WorldRotation; }
        }
        private Quaternion m_WorldRotation = Quaternion.LookRotation(Vector3.forward);

        /// <summary>
        /// Unrounded Scene GUI bounds (updated by custom editor).
        /// </summary>
        public Bounds EditorBounds
        {
            get { return m_EditorBounds; }
            set
            {
                m_EditorBounds = value;
                m_Offset = value.center;
                m_BoundsSize = value.size;

                RoundDetectionBoundsSize();
                m_DetectionBounds.center = value.center;
            }
        }
        [SerializeField, HideInInspector]
        private Bounds m_EditorBounds = new Bounds(Vector3.zero, new Vector3(20, 1, 20));

        /// <summary>
        /// Rounded detection bounds (actual sensor bounds).
        /// </summary>
        public Bounds DetectionBounds
        {
            get { return m_DetectionBounds; }
            set { m_DetectionBounds = value; }
        }
        [SerializeField, HideInInspector]
        private Bounds m_DetectionBounds = new Bounds(Vector3.zero, new Vector3(20, 1, 20));

        /// <summary>
        /// X/Z size of each grid cell.
        /// </summary>
        public float CellSize
        {
            get { return m_CellSize; }
            set { m_CellSize = value; }
        }
        [SerializeField, Tooltip("X/Z size of individual grid cells.")]
        [Min(0.1f)] private float m_CellSize = 1;
        [SerializeField, HideInInspector]
        private float m_TmpCellSize = 1;

        [SerializeField, Tooltip("The number of grid cells per axis.")]
        private Vector3Int m_NumCells = new Vector3Int(20, 1, 20);
        [SerializeField, ReadOnly, Tooltip("Actual detection bounds size of the grid sensor." +
            " Values are rounded to match cell size. Visualized by the blue box in scene view (Gizmos).")]
        private Vector3 m_DetectionSize = new Vector3(20, 1, 20);
        [SerializeField, Tooltip("Unrounded editor bounds, visualized by the white box in scene view (Gizmos)." +
            " Drag handles to change size. Key commands in scene GUI: "
            + "\nS - Snap to cell size\nC - Center on X-axis\nShift+C - Center on all axes")]
        private Vector3 m_BoundsSize = new Vector3(20, 1, 20);
        [SerializeField, Tooltip("Detection offset from sensor transform position.")]
        private Vector3 m_Offset;

        // TODO Update logic could be clearer.

        // Called by custom editor, either directly via key command
        // or indirectly by setting EditorBounds property.
        public void RoundDetectionBoundsSize()
        {
            Vector3 s = new Vector3(
                Mathf.Round(m_EditorBounds.size.x / m_CellSize), 0,
                Mathf.Round(m_EditorBounds.size.z / m_CellSize));
            s = Vector3.Max(s, Vector3.one);
            m_NumCells.x = (int)s.x;
            m_NumCells.y = 1;
            m_NumCells.z = (int)s.z;

            s *= m_CellSize;
            s.y = Mathf.Max(m_EditorBounds.size.y, 1);
            m_DetectionBounds.size = s;
            m_DetectionSize = s;

            m_GridSize.x = m_NumCells.x;
            m_GridSize.y = m_NumCells.z;
            UpdateObservationShapeInfo();
        }

        // Called by custom editor via key command.
        public void RoundDetectionBoundsCenter()
        {
            Vector3 c = new Vector3(
                Mathf.Round(m_EditorBounds.center.x / m_CellSize),
                Mathf.Round(m_EditorBounds.center.y / m_CellSize),
                Mathf.Round(m_EditorBounds.center.z / m_CellSize));
            m_DetectionBounds.center = c * m_CellSize;
        }

        // Called by custom editor via key command.
        public void CenterDetectionBoundsOnAxes(bool xOnly)
        {
            Vector3 c = Vector3.zero;
            c.y = xOnly ? m_DetectionBounds.center.y : c.y;
            c.z = xOnly ? m_DetectionBounds.center.z : c.z;
            m_DetectionBounds.center = c;
        }

        // Called by OnValidate after inspector update.
        protected override void UpdateSettings()
        {
            SyncEditorBoundsWithInspector();
            base.UpdateSettings();
        }

        protected override GameObjectDetector CreateDetector()
            => new GameObjectDetector2D()
            {
                BufferSize = m_ColliderBufferSize,
                ClearCacheOnReset = m_ClearCacheOnReset,
                Settings = m_Detectables,
                SensorTransform = transform,
                SensorOwner = GetComponentInParent<DetectableGameObject>(),
                Rotate = m_Rotate,
                WorldRotation = m_WorldRotation,
                Constraint = new Constraint2D()
                {
                    Bounds = m_DetectionBounds
                }
            };

        protected override GameObjectEncoder CreateEncoder()
            => new GameObjectEncoder2D()
            {
                Grid = m_PixelGrid,
                Settings = m_Detectables,
            };

        protected void SyncEditorBoundsWithInspector()
        {
            Bounds b = m_EditorBounds;

            if (m_TmpCellSize != m_CellSize)
            {
                // Cell size change -> keep bounds size x/z 
                // or grow to match single cell's size.
                m_TmpCellSize = m_CellSize;
                b.size = new Vector3(
                    Mathf.Max(b.size.x, m_CellSize),
                    b.size.y,
                    Mathf.Max(b.size.z, m_CellSize));
            }
            else if (b.center != m_Offset)
            {
                b.center = m_Offset;
            }
            else if (b.size != m_BoundsSize)
            {
                // Resize bounds.
                b.size = new Vector3(
                    Mathf.Max(m_BoundsSize.x, m_CellSize),
                    Mathf.Max(m_BoundsSize.y, 1),
                    Mathf.Max(m_BoundsSize.z, m_CellSize));
            }
            else
            {
                // Number of cells we can fit into bounds.
                Vector3Int n = new Vector3Int(
                    Mathf.RoundToInt(b.size.x / m_CellSize),
                    Mathf.RoundToInt(b.size.y / m_CellSize),
                    Mathf.RoundToInt(b.size.z / m_CellSize));

                if (n != m_NumCells)
                {
                    m_NumCells = Vector3Int.Max(m_NumCells, Vector3Int.one);
                    m_NumCells.y = 1; // 2D sensor

                    // Resize, fit x/z to cells.
                    Vector3 size = (Vector3)m_NumCells * m_CellSize;
                    size.y = b.size.y; // keep height
                    b.size = size;
                }
            }

            EditorBounds = b;
        }
    }
}
