using UnityEngine;
using UnityEditor;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using System;
using NaughtyAttributes;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Abstract base class for gameobject detecting grid sensor components.
    /// <see cref="GridSensorComponent2D"/> and <see cref="GridSensorComponent3D"/> 
    /// extend this class.
    /// </summary>
    public abstract class GridSensorComponentBaseGO : GridSensorComponentBase
    {
        protected abstract DetectorSpaceType DetectorSpaceType { get; }

        #region Detection Settings

        /// <summary>
        /// Whether to clear the <see cref="DetectableGameObject"/> cache on sensor reset 
        /// at the end of each episode. Should be disabled if <see cref="DetectableGameObject"/>s 
        /// don't change from one episode to the next.
        /// </summary>
        public bool ClearCacheOnReset
        {
            get { return m_ClearCacheOnReset; }
            set { m_ClearCacheOnReset = value; OnClearCacheChange(); }
        }
        [SerializeField]
        [OnValueChanged("OnClearCacheChange")]
        [Foldout("Detection")]
        [Label("Episode Reset")]
        [Tooltip("Whether to clear the detectable gameobject cache on sensor reset at " +
            "the end of each episode.\nShould be disabled if detectable gameobjects " +
            "don't change from one episode to the next.")]
        private bool m_ClearCacheOnReset;

        private void OnClearCacheChange()
        {
            if (HasSensor)
            {
                ((GameObjectDetector)m_GridSensor.Detector)
                    .ClearCacheOnReset = m_ClearCacheOnReset;
            }
        }

        /// <summary>
        /// The maximum number of colliders the sensor can detect at once.
        /// Buffer size will be doubled when maxed out.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        public int ColliderBufferSize
        {
            get { return m_ColliderBufferSize; }
            set { m_ColliderBufferSize = value; }
        }
        [SerializeField]
        [Foldout("Detection")]
        [Tooltip("The initial number of colliders the sensor can detect at once. " +
            "Buffer size will be doubled when maxed out.")]
        [Min(1)] private int m_ColliderBufferSize = 64;


        // Temp. value field for adding DetectableGameObjects.
        // Gets nulled after object is added to m_GameObjectSettingsList.
        [SerializeField]
        [OnValueChanged("TryAddDetectableObject")]
        [Foldout("Detection")]
        [Label("Add Detectable Object \u2794")]
        [Tooltip("Drag detectable gameobjects (prefab or scene) onto this field for "
            + "adding them to the settings list. Objects must have distinct tags.")]
        private DetectableGameObject m_TmpObjectToAdd;

        private void TryAddDetectableObject()
        {
            if (m_TmpObjectToAdd != null)
            {
                if (m_GameObjectSettingsMeta.TryAddDetectableObject(
                    m_GameObjectSettingsList, m_TmpObjectToAdd, DetectorSpaceType))
                {
                    ValidateGameObjectSettings();
                }
                m_TmpObjectToAdd = null;
            }
        }

        [SerializeField]
        [OnValueChanged("ValidateGameObjectSettings")]
        [Foldout("Detection")]
        [Label("Detectable Gameobject Settings By Tag")]
        [ShowIf("m_ShowGameObjectSettings")] 
        private List<GameObjectSettings> m_GameObjectSettingsList 
            = new List<GameObjectSettings>();
        // Flag for inspector visibility.
        private bool m_ShowGameObjectSettings;

        protected readonly GameObjectSettingsMeta m_GameObjectSettingsMeta
            = new GameObjectSettingsMeta();

        private void ValidateGameObjectSettings()
        {
            UpdateGridChannelCount(
                m_GameObjectSettingsMeta.Validate(m_GameObjectSettingsList));
            // Emptied list stays visible.
            m_ShowGameObjectSettings 
                = m_ShowGameObjectSettings || m_GameObjectSettingsList.Count > 0;
        }

        #endregion

        /// <inheritdoc/>
        public override ISensor[] CreateSensors()
        {
            DetectableGameObject.ClearCache();
            ValidateGameObjectSettings();

            if (m_GameObjectSettingsMeta.DetectableTags.Count > 0)
            {
                // Unlike GridSensorComponentBase, this component creates its own GridBuffer.
                // We use a ColorGridBuffer in order to support PNG compression.
                // GridShape is set by the 2D and 3D subcomponents prior to creating the buffer.
                GridBuffer = new ColorGridBuffer(GridShape);

                var sensors = base.CreateSensors();

                if (Application.isPlaying)
                {
                    // TODO It should be clearer which sensor features are enabled
                    // in editor vs play mode. Detector/Encoder code is supposed to 
                    // be runtime only. 
                    m_GridSensor.EnableAutoDetection(CreateDetector(), CreateEncoder());
                }

                return sensors;
            }

            throw new UnityAgentsException("No detectable tags found! " +
                "Add a detectable gameobject to the inspector settings.");
        }

        /// <summary>
        /// Invoked by GridEditorHelper.
        /// </summary>
        public virtual void OnEditorInit() 
        {
            ValidateGameObjectSettings();
        }


        protected abstract Encoder CreateEncoder();
        protected abstract GameObjectDetector CreateDetector();
        protected abstract DetectionConstraint CreateConstraint();


        /// <summary>
        /// Returns the detected gameobjects.
        /// </summary>
        /// <param name="tag">Tag for filtering gameobjects</param>
        /// <returns>Enumerable gameobjects</returns>
        public IEnumerable<GameObject> GetDetectedGameObjects(string tag)
        {
            if (HasSensor && m_GridSensor.Detector.Result.TryGetItems(
                tag, out IList<DetectionResult.Item> items))
            {
                for (int i = 0, n = items.Count; i < n; i++)
                {
                    yield return ((DetectableGameObject)items[i].Detectable).gameObject;
                }
            }
        }


        protected void SaveState(Action callback, string name)
        {
#if (UNITY_EDITOR)
            SaveStateEditor(callback, name);
#endif
        }


#if (UNITY_EDITOR)
        private HashSet<Action> m_UndoCallbacks;

        private void SaveStateEditor(Action callback, string name)
        {
            if (isActiveAndEnabled)
            {
                if (m_UndoCallbacks == null)
                {
                    m_UndoCallbacks = new HashSet<Action>();
                    Undo.undoRedoPerformed += OnUndoRedo;
                }

                Undo.RegisterCompleteObjectUndo(this, name);
                m_UndoCallbacks.Add(callback);
            }
        }

        private void OnUndoRedo()
        {
            var tmp = new Stack<Action>(m_UndoCallbacks);
            m_UndoCallbacks.Clear();

            while (tmp.Count > 0)
            {
                tmp.Pop().Invoke();
            }
        }

        private void ClearUndo()
        {
            m_UndoCallbacks?.Clear();
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        protected override void HandleValidate() 
        {
            // TODO Implement proper Undo/Redo handling for 
            // adding and removing detectable objects.
            ValidateGameObjectSettings();
        }
#endif


        protected override void HandleReset()
        {
            base.HandleReset();
            
            m_ShowGameObjectSettings = false;
            ValidateGameObjectSettings();

#if (UNITY_EDITOR)
            ClearUndo();
#endif
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();

#if (UNITY_EDITOR)
            ClearUndo();
#endif
        }
    }
}
