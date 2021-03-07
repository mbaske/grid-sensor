using UnityEngine;
using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    public class GameObjectEncoder3D : GameObjectEncoder
    {
        public DistanceNormalization DistanceNormalization
        {
            set { m_DistanceNormalization = value; }
        }
        protected DistanceNormalization m_DistanceNormalization;

        public override void Encode(DetectionResult result)
        {
            int tagChannel = m_NumChannels * m_CrntStackIndex; 
            m_Grid.ClearChannels(tagChannel, m_NumChannels);

            for (int iTag = 0, nTag = result.DetectableTags.Count; iTag < nTag; iTag++)
            {
                string tag = result.DetectableTags[iTag];
                bool hasObs = m_Settings.HasObservations(tag, out IList<int> obsIndices);

                if (result.TryGetItems(tag, out IList<DetectionResult.Item> items))
                {
                    ShapeModifier modifier = m_ModifiersByTag[tag];

                    for (int iItem = 0, nItem = items.Count; iItem < nItem; iItem++)
                    {
                        var item = items[iItem];
                        int channel = tagChannel;
                        modifier.Clear();

                        // Distance.
                        for (int iPoint = 0, nPoint = item.Points.Count; iPoint < nPoint; iPoint++)
                        {
                            Vector3 p = item.Points[iPoint];
                            Vector2Int pos = m_Grid.NormalizedToGridPos(p);

                            // Inverts z -> 0 (far) to 1 (near).
                            float norm = m_DistanceNormalization.Evaluate(p.z);
                            float prev = m_Grid.Read(channel, pos);
                            // Override if closer.
                            if (norm > prev)
                            {
                                m_Grid.Write(channel, pos, norm);
                                modifier.AddPoint(pos, p.z); // z: 0 (near) to 1 (far).
                            }
                            // else: ignore occluded.
                        }

                        modifier.Process(m_Grid, channel);
                        channel++;

                        // Additional observations.
                        if (hasObs)
                        {
                            for (int iObs = 0, nObs = obsIndices.Count; iObs < nObs; iObs++)
                            {
                                // The modifier already contains the required grid positions,
                                // even if we haven't applied any modifications (ShapeModNone).
                                modifier.Write(m_Grid, channel,
                                    item.Detectable.Observations.Evaluate(obsIndices[iObs]));
                                channel++;
                            }
                        }
                    }
                }

                tagChannel += hasObs ? obsIndices.Count + 1 : 1;
            }

            IncrementStackIndex();
        }
    }
}