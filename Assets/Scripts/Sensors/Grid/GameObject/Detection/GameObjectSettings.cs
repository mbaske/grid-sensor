using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// What to detect about a <see cref="DetectableGameObject"/>:
    /// <see cref="Position"/> - Transform position
    /// <see cref="ClosestPoint"/> - Closest point on collider(s)
    /// <see cref="Shape"/> - Set of points matching its shape
    /// </summary>
    public enum PointDetectionType
    {
        Position, ClosestPoint, Shape
    }

    /// <summary>
    /// Detection settings for a <see cref="DetectableGameObject"/> type (tag).
    /// A <see cref="DetectableGameObject"/> type is defined by its tag. Multiple sensors can 
    /// use different <see cref="PointDetectionType"/>s, <see cref="PointModifier"/>s and 
    /// <see cref="Observable"/>s for the same <see cref="DetectableGameObject"/> type.
    /// </summary>
    [System.Serializable]
    public class GameObjectSettings
    {
        /// <summary>
        /// The gameobject's tag.
        /// Although marked as hidden, it is displayed as the list item name.
        /// </summary>
        [HideInInspector]
        public string Tag;

        /// <summary>
        /// Whether this <see cref="DetectableGameObject"/> type is included in sensor observations.
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        [Tooltip("Whether this detectable object type is included in sensor observations.")]
        public bool Enabled = true;

        /// <summary>
        /// What to detect about a <see cref="DetectableGameObject"/>.
        /// </summary>
        [Tooltip("What to detect about the gameobject.")]
        public PointDetectionType DetectionType;
        // Flag for inspector enable.
        private bool UsesShapeDetection => DetectionType == PointDetectionType.Shape;

        /// <summary>
        /// How to fill space between points for <see cref="PointDetectionType.Shape"/>
        /// Note that changing this after the sensor is created has no effect.
        /// </summary>
        [ShowIf("UsesShapeDetection")]
        [AllowNesting]
        [Tooltip("How to fill space between points\nfor Detection Type = Shape.")]
        public PointModifierType Modifier;

        /// <summary>
        /// The <see cref="DetectableGameObject"/> the settings are associated with.
        /// 
        /// Note that the field refers to a concrete instance (which was dragged 
        /// onto the inspector field), because the settings object needs to read  
        /// its tag and observables. However, the settings subsequently apply to   
        /// ALL <see cref="DetectableGameObject"/> instances sharing the same tag.
        /// </summary>
        [HideInInspector]
        public DetectableGameObject Detectable;

        // Copy of original observables created by the DetectableGameObject.
        [SerializeField, HideInInspector]
        private ObservableCollection m_UserObservables;

        // List of user defined + dedicated observables. Items can be
        // enabled/disabled and reordered. Determines encoding of observables.
        [SerializeField]
        [Tooltip("A detectable gameobject requires at least one observable.")]
        private List<Observable> m_Observables = new List<Observable>();

        [SerializeField, HideInInspector]
        private DetectorSpaceType m_DetectorSpaceType;

        /// <summary>
        /// Creates the <see cref="GameObjectSettings"/> instance.
        /// </summary>
        /// <param name="detectable"><see cref="DetectableGameObject"/> 
        /// the <see cref="GameObjectSettings"/> apply to</param>
        public GameObjectSettings(DetectableGameObject detectable, DetectorSpaceType detectorSpaceType)
        {
            Detectable = detectable;
            m_DetectorSpaceType = detectorSpaceType;
        }

        /// <summary>
        /// Validates the <see cref="GameObjectSettings"/> instance.
        /// </summary>
        public void Validate()
        {
            // Tag might have changed.
            Tag = Detectable.Tag;

            CheckUserObservablesChanged();
            int n = m_Observables.Count;

            if (n == 0)
            {
                // New or emptied settings:
                // Will add distance (3D) or one-hot (2D) as default.
                AddDedicatedObservable();
            }
            else
            {
                CheckDuplicateEntry(n);
            }

            ValidateDistanceObservableIndex();
        }

        /// <summary>
        /// Enumerates enabled <see cref="Observable"/>s.
        /// Will tell the <see cref="Encoder"/> which 
        /// <see cref="Observable"/>s to query an in what order.
        /// </summary>
        /// <returns><see cref="Observable"/> enumeration</returns>
        public IEnumerable<Observable> GetEnabledObservables()
        {
            foreach (var obs in m_Observables)
            {
                if (obs.Enabled)
                {
                    yield return obs;
                }
            }
        }

        private void CheckUserObservablesChanged()
        {
            // Re-initialize in case of code changes.
            var current = Detectable.InitObservables();

            if (!current.Equals(m_UserObservables))
            {
                bool hasDistance = HasObservableType(ObservableType.Distance);
                bool hasOneHot = HasObservableType(ObservableType.OneHot);

                m_UserObservables = current.Copy();
                // Overwrite with changed...
                m_UserObservables.CopyTo(m_Observables); 

                // and re-add dedicated.
                if (hasDistance)
                {
                    AddDedicatedObservable();
                }
                if (hasOneHot)
                {
                    AddDedicatedObservable();
                }
            }
        }

        // User can press list + for adding dedicated observables
        // or for re-adding previously removed user observables.
        private void CheckDuplicateEntry(int n)
        {
            bool HasDuplicate = n > 1 && 
                m_Observables[n - 1].Name == m_Observables[n - 2].Name;

            if (HasDuplicate)
            {
                // NOTE RemoveAt(n - 1) must come AFTER 'can add'
                // checks, but BEFORE adding new observables.

                if (CanAddDistanceObservable())
                {
                    // Replace duplicate with distance.
                    m_Observables.RemoveAt(n - 1);
                    AddDedicatedObservable();
                }
                else if (CanAddUserObservable(out Observable obs))
                {
                    // Replace duplicate with user observable.
                    m_Observables.RemoveAt(n - 1);
                    m_Observables.Add(obs);
                }
                else if (CanAddOneHotObservable())
                {
                    // Replace duplicate with one-hot.
                    m_Observables.RemoveAt(n - 1);
                    AddDedicatedObservable();
                }
                else
                {
                    // Nothing to add.
                    m_Observables.RemoveAt(n - 1);
                }
            }
        }

        private void AddDedicatedObservable()
        {
            // Distance can only be added to 3D.
            if (CanAddDistanceObservable())
            {
                m_Observables.Insert(0, 
                    new Observable(ObservableType.Distance, Observable.Distance));
                // Add one at a time.
                return;
            }

            // One-Hot can be added to 3D and 2D.
            if (CanAddOneHotObservable())
            {
                m_Observables.Add(
                    new Observable(ObservableType.OneHot, Observable.OneHot));
            }
        }

        private bool CanAddDistanceObservable()
        {
            return m_DetectorSpaceType == DetectorSpaceType.Sphere &&
                !HasObservableType(ObservableType.Distance);
        }

        private bool CanAddOneHotObservable()
        {
            return !HasObservableType(ObservableType.OneHot);
        }

        private bool CanAddUserObservable(out Observable next)
        {
            if (m_UserObservables.ContainsOtherThan(
                m_Observables, out IList <Observable> list))
            {
                next = list[0];
                return true;
            }

            next = null;
            return false;
        }


        // NOTE Distance always comes first.
        // User might have switched entries.
        private void ValidateDistanceObservableIndex()
        {
            int index = m_Observables.FindIndex(
                o => o.Type == ObservableType.Distance);

            if (index > 0)
            {
                var obs = m_Observables[index];
                m_Observables.RemoveAt(index);
                m_Observables.Insert(0, obs);

                Debug.LogWarning("Reordering observables: distance needs to come first.");
            }
        }

        private bool HasObservableType(ObservableType type)
        {
            return m_Observables.Any(o => o.Type == type);
        }
    }
}