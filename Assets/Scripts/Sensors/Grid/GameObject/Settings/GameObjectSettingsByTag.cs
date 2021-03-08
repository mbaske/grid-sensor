using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace MBaske.Sensors.Grid
{
    [System.Serializable]
    public class GameObjectSettingsByTag : IEncodingSettings
    {
        // IEncodingSettings

        public IList<string> DetectableTags => m_Tags;

        public bool HasObservations(string tag, out IList<int> indices)
            => m_ObservationIndices.TryGetValue(tag, out indices);

        public ShapeModifierType GetShapeModifierType(string tag)
            => m_ModifierTypes[tag];


        // GameObjectDetector specific.

        public int LayerMask { get; private set; }

        public bool IsDetectableTag(string tag, out ColliderDetectionType detectionType)
            => m_DetectionTypes.TryGetValue(tag, out detectionType);



        [SerializeField, Tooltip("Tag of the detectable gameobject.")]
        private List<GameObjectSettings> m_SettingsByTag
            = new List<GameObjectSettings>();

        private readonly List<string> m_Tags = new List<string>();

        private readonly IDictionary<string, IList<int>> m_ObservationIndices
            = new Dictionary<string, IList<int>>();

        private readonly IDictionary<string, ShapeModifierType> m_ModifierTypes
            = new Dictionary<string, ShapeModifierType>();

        private readonly IDictionary<string, ColliderDetectionType> m_DetectionTypes
            = new Dictionary<string, ColliderDetectionType>();


        public bool TryAddGameObject(GameObject obj)
        {
            var comp = obj.GetComponentInChildren<DetectableGameObject>();

            if (comp != null)
            {
                if (!DetectableTags.Contains(comp.Tag))
                {
                    m_SettingsByTag.Add(new GameObjectSettings(comp));
                    return true;
                }
                else
                {
                    Debug.LogWarning($"Tag '{comp.Tag}' already added, tags must be distinct.");
                }
            }
            else
            {
                Debug.LogError($"{obj} is not a detectable gameobject. Add a DetectableGameObject component.");
            }

            return false;
        }

        public void Update(GridType type)
        {
            LayerMask = 0;
            m_Tags.Clear();
            m_ObservationIndices.Clear();
            m_ModifierTypes.Clear();
            m_DetectionTypes.Clear();

            for (int i = m_SettingsByTag.Count - 1; i >= 0; i--)
            {
                GameObjectSettings settings = m_SettingsByTag[i];

                if (settings.Detectable == null)
                {
                    m_SettingsByTag.RemoveAt(i);
                }
                else if (settings.Enabled)
                {
                    settings.Update();
                    string tag = settings.Tag;

                    if (m_Tags.Contains(tag))
                    {
                        // Can occur if tag is changed on object that was already added.
                        Debug.LogError($"Duplicate tag '{tag}' found, tags must be distinct.");
                    }
                    else
                    {
                        m_Tags.Add(tag);
                        m_DetectionTypes.Add(tag, settings.DetectionType);
                        m_ModifierTypes.Add(tag, settings.DetectionType == ColliderDetectionType.Shape
                            ? settings.ShapeModifier : ShapeModifierType.None);

                        if (settings.ObservationsCount > 0)
                        {
                            m_ObservationIndices.Add(tag, settings.GetObservationIndices());
                        }
                        else if (type == GridType._2D)
                        {
                            Debug.LogError("No observations found in detectable object '" + tag
                                + "'. 2D detection requires at least one observation.");
                        }
                    }

                    LayerMask |= 1 << settings.Detectable.gameObject.layer;
                }
            }
            m_Tags.Reverse();
        }

        public int GetRequiredChannelsCount(GridType type)
        {
            int nSum = 0;

            for (int i = 0, n = m_SettingsByTag.Count; i < n; i++)
            {
                if (m_SettingsByTag[i].Enabled)
                {
                    int nObs = m_SettingsByTag[i].ObservationsCount;

                    if (type == GridType._3D)
                    {
                        // 3D detection always includes a distance channel.
                        nObs++;
                    }

                    nSum += nObs;
                }
            }

            return nSum;
        }
    }
}