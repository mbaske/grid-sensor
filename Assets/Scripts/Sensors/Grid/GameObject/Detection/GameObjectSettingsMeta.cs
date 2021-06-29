using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Validates <see cref="GameObjectSettings"/> and stores them by tag.
    /// </summary>
    public class GameObjectSettingsMeta : IEncodingSettings
    {
        #region IEncodingSettings

        /// <inheritdoc/>
        public IList<string> DetectableTags => m_Tags;

        /// <inheritdoc/>
        public IEnumerable<Observable> GetObservables(string tag)
        {
            if (m_Observables.TryGetValue(tag, out IEnumerable<Observable> result))
            {
                return result;
            }
            // Catch debug runtime deletion -> will bypass encoding.
            return new Observable[0];
        }

        /// <inheritdoc/>
        public PointModifierType GetPointModifierType(string tag)
        {
            return m_PointModifierTypes[tag];
        }

        #endregion


        #region GameObjectDetector specific

        /// <summary>
        /// The combined detection mask for all <see cref="DetectableGameObject"/>s.
        /// </summary>
        public int LayerMask { get; private set; }

        /// <summary>
        /// Whether the specified tag is associated with a <see cref="DetectableGameObject"/> type.
        /// </summary>
        /// <param name="tag">The specified tag</param>
        /// <param name="detectionType">The <see cref="PointDetectionType"/> 
        /// associated with the tag (output)</param>
        /// <returns>True if the tag is associated with a <see cref="DetectableGameObject"/></returns>
        public bool IsDetectableTag(string tag, out PointDetectionType detectionType)
            => m_DetectionTypes.TryGetValue(tag, out detectionType);

        #endregion


        // Store tags so we don't need to repeatedly search for them in the settings.
        private readonly List<string> m_Tags = new List<string>();

        // Settings for all detectable objects, by tag:

        private readonly IDictionary<string, IEnumerable<Observable>> m_Observables
            = new Dictionary<string, IEnumerable<Observable>>();

        private readonly IDictionary<string, PointModifierType> m_PointModifierTypes
            = new Dictionary<string, PointModifierType>();

        private readonly IDictionary<string, PointDetectionType> m_DetectionTypes
            = new Dictionary<string, PointDetectionType>();


        /// <summary>
        /// Adds a <see cref="DetectableGameObject"/> to the settings.
        /// </summary>
        /// <param name="settingsList">List of <see cref="GameObjectSettings"/></param>
        /// <param name="obj"><see cref="DetectableGameObject"/> to add</param>
        /// <param name="detectorSpaceType"><see cref="DetectorSpaceType"/>, 
        /// box (2D) or sphere (3D)</param>
        /// <returns>If <see cref="GameObjectSettings"/> were created for 
        /// <see cref="DetectableGameObject"/></returns>
        public bool TryAddDetectableObject(
            List<GameObjectSettings> settingsList, 
            DetectableGameObject obj,
            DetectorSpaceType detectorSpaceType)
        {
            if (!m_Tags.Contains(obj.Tag))
            {
                settingsList.Add(new GameObjectSettings(obj, detectorSpaceType));
                return true;
            }

            Debug.LogWarning($"Tag '{obj.Tag}' already added, tags must be distinct.");
            return false;
        }

        /// <summary>
        /// Validates list of <see cref="GameObjectSettings"/>, stores them by tag.
        /// </summary>
        /// <param name="settingsList">List of <see cref="GameObjectSettings"/></param>
        /// <returns>The number of required observation channels</returns>
        public int Validate(List<GameObjectSettings> settingsList)
        {
            LayerMask = 0;
            int numReqChannels = 0;

            m_Tags.Clear();
            m_Observables.Clear();
            m_DetectionTypes.Clear();
            m_PointModifierTypes.Clear();

            for (int i = settingsList.Count - 1; i >= 0; i--)
            {
                GameObjectSettings settings = settingsList[i];

                if (settings.Detectable == null)
                {
                    // Remove null references from list.
                    settingsList.RemoveAt(i);
                }
                else
                {
                    settings.Validate();

                    if (settings.Enabled)
                    {
                        string tag = settings.Tag;

                        if (m_Tags.Contains(tag))
                        {
                            settingsList.RemoveAt(i);
                            // Can occur if tag is changed on object that was already added,
                            // or if entry was duplicated by pressing list +.
                            Debug.LogWarning($"Duplicate tag '{tag}' found, tags must be distinct. " +
                                "Removing detectable object from list.");
                        }
                        else
                        {
                            m_Tags.Add(tag);
                            m_DetectionTypes.Add(tag, settings.DetectionType);
                            m_PointModifierTypes.Add(tag,
                                settings.DetectionType == PointDetectionType.Shape
                                ? settings.Modifier : PointModifierType.None);

                            var obs = settings.GetEnabledObservables();
                            int numObs = obs.Count();
                            if (numObs == 0)
                            {
                                Debug.LogError($"No enabled observables found for tag '{tag}'. " +
                                    "A detectable gameobject requires at least one observable.");
                            }
                            else
                            {
                                m_Observables.Add(tag, obs);
                                numReqChannels += numObs;
                            }
                        }

                        LayerMask |= 1 << settings.Detectable.gameObject.layer;
                    }
                }
            }

            // Was iterating down before.
            m_Tags.Reverse();

            return numReqChannels;
        }
    }
}