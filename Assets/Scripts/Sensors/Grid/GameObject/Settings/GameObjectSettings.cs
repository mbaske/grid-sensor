using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    public enum ColliderDetectionType
    {
        Position, ClosestPoint, Shape
    }

    [System.Serializable]
    public class GameObjectSettings
    {
        [HideInInspector]
        public string Tag;
        [Tooltip("Whether to enable detection for this gameobject type.")]
        public bool Enabled = true;
        // Multiple sensors can use different detection types and modifiers
        // for the same object.
        [Tooltip("What to detect about the gameobject.")]
        public ColliderDetectionType DetectionType;
        [Tooltip("How to fill space between points\nfor Detection Type = Shape.")]
        public ShapeModifierType ShapeModifier;
        [HideInInspector]
        public DetectableGameObject Detectable;

        public int ObservationsCount => m_ObservationsList.Count;

        [SerializeField, HideInInspector]
        private Observations m_Observations;
        // List of all exposed observation names. 
        [SerializeField, HideInInspector]
        private List<string> m_AllObservations = new List<string>();
        // Selected names list, used for filtering observations in inspector.
        // -> Multiple sensors can get different observations from same object.
        [SerializeField, ReadOnly, Tooltip(
            "Observations for gameobject. 2D detection requires at least one observation."
            + "For 3D, the list can be empty since the object's distance from the sensor"
            + "is being observed by default.")]
        private List<string> m_ObservationsList = new List<string>();

        public GameObjectSettings(DetectableGameObject obj)
        {
            Detectable = obj;
        }

        public void Update()
        {
            Tag = Detectable.Tag;
            m_Observations = Detectable.InitObservations();

            if (m_Observations.HasObservations(out IList<string> names))
            {
                bool hasEmpty = m_ObservationsList.Contains("");
                bool hasDuplicates = m_ObservationsList.Count
                    > m_ObservationsList.Distinct().Count();
                // true if exposed observations were changed in detectable class.
                bool hasChange = !Enumerable.SequenceEqual(m_AllObservations, names);

                if (hasEmpty || hasDuplicates || hasChange)
                {
                    // Show all observation names in inspector.
                    m_AllObservations.Clear();
                    m_AllObservations.AddRange(names);
                    m_ObservationsList.Clear();
                    m_ObservationsList.AddRange(names);
                }
            }
            else
            {
                m_AllObservations.Clear();
                m_ObservationsList.Clear();
            }
        }

        // Matches all exposed observations with selected names list.
        // Will tell the encoder what observations to query and in what order.
        public IList<int> GetObservationIndices()
        {
            int n = m_ObservationsList.Count;
            var list = new List<int>(n);
            for (int i = 0; i < n; i++)
            {
                list.Add(m_Observations.GetIndex(m_ObservationsList[i]));
            }
            return list;
        }
    }
}