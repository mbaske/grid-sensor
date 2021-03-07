using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    public enum GridType
    {
        _2D, _3D
    }

    public abstract class GridSensorComponent : GridSensorComponentBase, IPixelGridProvider
    {
        protected abstract GridType GridType { get; }

        /// <summary>
        /// The number of stacked observations. Enable stacking (value > 1) 
        /// if agents need to infer movement from observations.
        /// </summary>
        public int StackedObservations
        {
            get { return m_StackedObservations; }
            set { m_StackedObservations = value; Validate(); }
        }
        [SerializeField]
        [Tooltip("The number of stacked observations. Enable stacking (value > 1) "
            + "if agents need to infer movement from observations.")]
        protected int m_StackedObservations = 1;

        /// <summary>
        /// The maximum number of colliders the sensor can detect at once.
        /// </summary>
        public int ColliderBufferSize
        {
            get { return m_ColliderBufferSize; }
            set { m_ColliderBufferSize = value; Validate(); }
        }
        [SerializeField]
        [Tooltip("The maximum number of colliders the sensor can detect at once.")]
        protected int m_ColliderBufferSize = 64;

        /// <summary>
        /// Whether to clear the collider cache on sensor reset (end of episode). 
        /// Should be disabled if colliders don't change from one episode to the next.
        /// </summary>
        public bool ClearCacheOnReset
        {
            get { return m_ClearCacheOnReset; }
            set { m_ClearCacheOnReset = value; Validate(); }
        }
        [SerializeField]
        [Tooltip("Whether to clear the collider cache on sensor reset (end of episode).")]
        protected bool m_ClearCacheOnReset;

        // Temp. value field for adding DetectableGameObjects.
        // Gets nulled after object is added to m_DetectableTags.
        [SerializeField, Tooltip("Drag detectable gameobjects (prefab or scene) onto this "
            + "field for adding them to the settings list. Objects must have distinct tags.")]
        private GameObject m_AddDetectableObject;

        /// <summary>
        /// Detection settings for DetectableGameObjects.
        /// Objects are added in inspector via drag&drop.
        /// </summary>
        public GameObjectSettingsByTag GameObjectSettingsByTag
        {
            get { return m_Detectables; }
            set { m_Detectables = value; Validate(); }
        }

        [SerializeField, Tooltip("Detectable gameobjects by tag.")]
        protected GameObjectSettingsByTag m_Detectables = new GameObjectSettingsByTag();

        /// <summary>
        /// The size of the observed grid.
        /// </summary>
        public Vector2Int GridSize
        {
            get { return m_GridSize; }
        }
        [SerializeField, HideInInspector]
        protected Vector2Int m_GridSize = Vector2Int.one * 20;

        // Unlike GridSensorComponentBase, 
        // this component generates its own PixelGrid
        // and therefore implements IPixelGridProvider.
        protected PixelGrid m_PixelGrid;

        protected abstract GameObjectDetector CreateDetector();
        protected abstract GameObjectEncoder CreateEncoder();

        protected override void UpdateSettings()
        {
            //DetectableGameObject.ClearCache();

            if (m_AddDetectableObject != null)
            {
                m_Detectables.TryAddGameObject(m_AddDetectableObject);
                m_AddDetectableObject = null;
            }
            m_Detectables.Update(GridType);
        }

        
        public override ISensor CreateSensor()
        {
            base.CreateSensor();
            Sensor.Detector = CreateDetector();
            Sensor.Encoder = CreateEncoder();
            return Sensor;
        }

        public override void UpdateSensor()
        {
            if (Sensor != null)
            {
                m_PixelGrid = new PixelGrid(GetGridShape());
                base.UpdateSensor();

                Sensor.Detector = CreateDetector();
                Sensor.Encoder = CreateEncoder();
            }
        }

        public override PixelGrid GetPixelGrid()
        {
            m_PixelGrid ??= new PixelGrid(GetGridShape());
            return m_PixelGrid;
        }

        public override GridShape GetGridShape()
        {
            return new GridShape(m_StackedObservations,
                m_Detectables.GetRequiredChannelsCount(GridType),
                m_GridSize);
        }

        public IEnumerable<GameObject> GetDetectedGameObjects(string tag)
        {
            if (Sensor?.Detector != null && Sensor.Detector.Result.TryGetItems(
                tag, out IList<DetectionResult.Item> items))
            {
                for (int i = 0, n = items.Count; i < n; i++)
                {
                    yield return ((DetectableGameObject)items[i].Detectable).gameObject;
                }
            }
        }
    }
}
