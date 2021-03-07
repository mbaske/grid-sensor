using UnityEngine;
using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    public class GameObjectEncoder2D : GameObjectEncoder
    {
        public override void Encode(DetectionResult result)
        {
            int tagChannel = m_NumChannels * m_CrntStackIndex;
            m_Grid.ClearChannels(tagChannel, m_NumChannels);

            for (int iTag = 0, nTag = result.DetectableTags.Count; iTag < nTag; iTag++)
            {
                string tag = result.DetectableTags[iTag];

                if (m_Settings.HasObservations(tag, out IList<int> obsIndices))
                {
                    if (result.TryGetItems(tag, out IList<DetectionResult.Item> items))
                    {
                        ShapeModifier modifier = m_ModifiersByTag[tag];

                        for (int iItem = 0, nItem = items.Count; iItem < nItem; iItem++)
                        {
                            var item = items[iItem];
                            int channel = tagChannel;
                            modifier.Clear();

                            // First observation.
                            float value = item.Detectable.Observations.Evaluate(obsIndices[0]);

                            for (int iPoint = 0, nPoint = item.Points.Count; iPoint < nPoint; iPoint++)
                            {
                                Vector2Int pos = m_Grid.NormalizedToGridPos(item.Points[iPoint]);
                                m_Grid.Write(channel, pos, value);
                                modifier.AddPoint(pos);
                            }

                            modifier.Process(m_Grid, channel);
                            channel++;

                            // Additional observations.
                            if (obsIndices.Count > 1)
                            {
                                for (int iObs = 1, nObs = obsIndices.Count; iObs < nObs; iObs++)
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
                }
                else throw new UnityException("No observations found for " + tag);

                tagChannel += obsIndices.Count;
            }

            IncrementStackIndex();
        }
    }
}