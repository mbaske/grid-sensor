using UnityEngine;
using NaughtyAttributes;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Sensor component for detecting gameobjects on a plane.
    /// </summary>
    [HelpURL("https://github.com/mbaske/grid-sensor")]
    [RequireComponent(typeof(GridEditorHelper2D))]
    public class GridSensorComponent2D : GridSensorComponentBaseGO
    {
        protected override DetectorSpaceType DetectorSpaceType => DetectorSpaceType.Box;


        #region Geometry Settings

        /// <summary>
        /// Whether and how to rotate detection bounds with the sensor component.
        /// </summary>
        public GameObjectDetector2D.SensorRotationType RotationType
        {
            get { return m_RotationType; }
            set { m_RotationType = value; OnRotationTypeChange();  }
        }
        [SerializeField]
        [OnValueChanged("OnRotationTypeChange")]
        [Foldout("Geometry")]
        [Tooltip("Sensor rotation type.")]
        private GameObjectDetector2D.SensorRotationType m_RotationType 
            = GameObjectDetector2D.SensorRotationType.AgentY;
        // Default world rotation.
        private Quaternion m_WorldRotation = Quaternion.LookRotation(Vector3.forward);
    
        private void OnRotationTypeChange()
        {
            if (HasSensor)
            {
                ((GameObjectDetector2D)m_GridSensor.Detector)
                    .RotationType = RotationType;
            }
        }


        /// <summary>
        /// Rounded detection bounds (actual sensor bounds).
        /// </summary>
        public Bounds DetectionBounds
        {
            get { return m_DetectionBounds; }
        }
        [SerializeField, HideInInspector]
        private Bounds m_DetectionBounds = new Bounds(Vector3.zero, new Vector3(20, 1, 20));


        /// <summary>
        /// Unrounded Scene GUI bounds used for editing.
        /// </summary>
        public Bounds EditorBounds
        {
            get { return m_EditorBounds; }
            set 
            {
                m_EditorBounds = value; 
                OnEditorBoundsChange();
            }
        }
        [SerializeField, HideInInspector]
        private Bounds m_EditorBounds = new Bounds(Vector3.zero, new Vector3(20, 1, 20));

        private void OnEditorBoundsChange()
        {
            m_Offset = m_EditorBounds.center;
            m_BoundsSize = m_EditorBounds.size;
            m_DetectionBounds.center = m_EditorBounds.center;

            RoundDetectionBoundsSize();

            // Saving for Scene GUI Undo.
            SaveState(OnEditorBoundsChange, "Grid Sensor Change");
        }


        /// <summary>
        /// X/Z size of each grid cell.
        /// </summary>
        public float CellSize
        {
            get { return m_CellSize; }
        }
        [SerializeField]
        [OnValueChanged("OnCellSizeChange")]
        [Foldout("Geometry")]
        [Tooltip("X/Z size of individual grid cells.")]
        [Min(0.1f)] private float m_CellSize = 1;
        
        private void OnCellSizeChange()
        {
            Bounds b = m_EditorBounds;
            b.size = new Vector3(
                Mathf.Max(b.size.x, m_CellSize),
                b.size.y,
                Mathf.Max(b.size.z, m_CellSize));
            EditorBounds = b;
        }


        [SerializeField]
        [OnValueChanged("OnNumCellsChange")]
        [Foldout("Geometry")]
        [Tooltip("The number of grid cells per axis.")]
        private Vector3Int m_NumCells = new Vector3Int(20, 1, 20);

        private void OnNumCellsChange()
        {
            Bounds b = m_EditorBounds;
            // Number of cells we can fit into bounds.
            Vector3Int n = new Vector3Int(
                Mathf.RoundToInt(b.size.x / m_CellSize),
                Mathf.RoundToInt(b.size.y / m_CellSize),
                Mathf.RoundToInt(b.size.z / m_CellSize));

            if (n != m_NumCells)
            {
                m_NumCells = Vector3Int.Max(m_NumCells, Vector3Int.one);
                m_NumCells.y = 1;

                // Resize, fit x/z to cells.
                Vector3 size = (Vector3)m_NumCells * m_CellSize;
                size.y = b.size.y; // keep height
                b.size = size;
            }
            EditorBounds = b;
        }


        [SerializeField]
        [ReadOnly]
        [Foldout("Geometry")]
        [Tooltip("Actual detection bounds size of the grid sensor. Values are rounded " +
            "to match cell size. Visualized by the blue box in scene view (Gizmos).")]
        private Vector3 m_DetectionSize = new Vector3(20, 1, 20);


        [SerializeField]
        [OnValueChanged("OnBoundsSizeChange")]
        [Foldout("Geometry")]
        [Tooltip("Unrounded editor bounds, visualized by the white box in scene view (Gizmos)." +
            " Drag handles to change size. Key commands in scene GUI: "
            + "\nS - Snap to cell size\nC - Center on X-axis\nShift+C - Center on all axes")]
        private Vector3 m_BoundsSize = new Vector3(20, 1, 20);

        private void OnBoundsSizeChange()
        {
            Bounds b = m_EditorBounds;
            b.size = new Vector3(
                Mathf.Max(m_BoundsSize.x, m_CellSize),
                Mathf.Max(m_BoundsSize.y, 1),
                Mathf.Max(m_BoundsSize.z, m_CellSize));
            EditorBounds = b;
        }


        [SerializeField]
        [OnValueChanged("OnOffsetChange")]
        [Foldout("Geometry")]
        [Tooltip("Detection offset from sensor transform position.")]
        private Vector3 m_Offset;

        private void OnOffsetChange()
        {
            Bounds b = m_EditorBounds;
            b.center = m_Offset;
            EditorBounds = b;
        }

        #endregion


        // Invoked after setting EditorBounds property.
        private void RoundDetectionBoundsSize()
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

            UpdateGridSize(m_NumCells.x, m_NumCells.z);
        }

        /// <summary>
        /// Invoked by custom editor via key command.
        /// </summary>
        public void RoundDetectionBoundsCenter()
        {
            Vector3 c = new Vector3(
                Mathf.Round(m_EditorBounds.center.x / m_CellSize),
                Mathf.Round(m_EditorBounds.center.y / m_CellSize),
                Mathf.Round(m_EditorBounds.center.z / m_CellSize));
            m_DetectionBounds.center = c * m_CellSize;

            // Saving for Scene GUI Undo.
            SaveState(OnEditorBoundsChange, "Grid Sensor Change");
        }

        /// <summary>
        /// Invoked by custom editor via key command.
        /// </summary>
        public void CenterDetectionBoundsOnAxes(bool xOnly)
        {
            Vector3 c = Vector3.zero;
            c.y = xOnly ? m_DetectionBounds.center.y : c.y;
            c.z = xOnly ? m_DetectionBounds.center.z : c.z;
            m_DetectionBounds.center = c;

            // Saving for Scene GUI Undo.
            SaveState(OnEditorBoundsChange, "Grid Sensor Change");
        }

        /// <summary>
        /// Get rotation for the selected <see cref="GameObjectDetector2D.SensorRotationType"/>.
        /// </summary>
        /// <returns>The current rotation applied to the detector</returns>
        public Quaternion GetRotation()
        {
            return m_RotationType switch
            {
                GameObjectDetector2D.SensorRotationType.AgentY 
                    => Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up),
                GameObjectDetector2D.SensorRotationType.AgentXYZ 
                    => transform.rotation,
                  _ => m_WorldRotation,
            };
        }

        /// <summary>
        /// Invoked by <see cref="GridEditorHelper2D"/>.
        /// </summary>
        public override void OnEditorInit() 
        {
            base.OnEditorInit();
            // Write back serialized value, will save 
            // initial state and update grid shape info.
            EditorBounds = m_EditorBounds;
        }

        protected override GameObjectDetector CreateDetector()
        {
            return new GameObjectDetector2D()
            {
                Result = new DetectionResult(
                    m_GameObjectSettingsMeta.DetectableTags, 
                    ColliderBufferSize),

                Settings = m_GameObjectSettingsMeta,
                ColliderBufferSize = ColliderBufferSize,
                ClearCacheOnReset = ClearCacheOnReset,
                SensorTransform = transform,
                SensorOwner = GetComponentInParent<DetectableGameObject>(),
                RotationType = RotationType,
                WorldRotation = m_WorldRotation,
                Constraint = (DetectionConstraint2D)CreateConstraint()
            };
        }

        protected override Encoder CreateEncoder()
        {
            return new Encoder()
            {
                Settings = m_GameObjectSettingsMeta,
                GridBuffer = GridBuffer
            };
        }

        protected override DetectionConstraint CreateConstraint()
        { 
            return new DetectionConstraint2D()
            {
                Bounds = DetectionBounds
            };
        }
    }
}
