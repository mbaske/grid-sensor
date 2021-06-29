using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MBaske.Sensors.Grid
{
    /// The encoder is responsible for writing <see cref="DetectionResult"/> contents
    /// to the <see cref="Grid.GridBuffer"/> which is used by the <see cref="GridSensor"/>.
    public class Encoder : IEncoder, IDebugable
    {
        /// <summary>
        /// <see cref="Grid.DistanceNormalization"/> is specific to this <see cref="Encoder"/>.
        /// </summary>
        public DistanceNormalization DistanceNormalization
        {
            get { return m_Normalization; }
            set { m_Normalization = value; }
        }
        protected DistanceNormalization m_Normalization;


        #region IEncoder Properties

        /// <inheritdoc/>
        public IEncodingSettings Settings
        {
            get { return m_Settings; }
            set { m_Settings = value; CreatePointModifiers(); }
        }
        protected IEncodingSettings m_Settings;

        /// <inheritdoc/>
        public GridBuffer GridBuffer
        {
            set { m_GridBuffer = value; CreatePointModifiers(); }
        }
        protected GridBuffer m_GridBuffer;

        #endregion

        private void CreatePointModifiers()
        {
            if (m_Settings != null && m_GridBuffer != null)
            {
                m_PointModifiersByTag = PointModifier.CreateModifiers(m_Settings, m_GridBuffer);
            }
        }

        // Each detectable object type gets its own PointModifier.
        protected IDictionary<string, PointModifier> m_PointModifiersByTag;

        
        // Debugging.

        protected bool m_Debug_IsEnabled;
        protected DebugChannelData m_Debug_ChannelData;

        /// <inheritdoc/>
        public void SetDebugEnabled(bool enabled, DebugChannelData target = null)
        {
            m_Debug_IsEnabled = enabled;

            if (enabled)
            {
                m_Debug_ChannelData = target;
            }
        }


        /// <inheritdoc/>
        public void Encode(DetectionResult result)
        {
            m_GridBuffer.Clear();

            if (m_Debug_IsEnabled)
            {
                m_Debug_ChannelData.ClearGridPositions();
            }

            int firstTagChannel = 0;
            
            // TODO Too many nested loops.

            foreach (var tag in result.DetectableTags)
            {
                // Observables for current tag.
                var tagObs = m_Settings.GetObservables(tag);

                if (result.TryGetItems(tag, out IList<DetectionResult.Item> tagItems))
                {
                    // Has detection result for current tag -> get matching modifier.
                    var modifier = m_PointModifiersByTag[tag];
               
                    foreach (var item in tagItems)
                    {
                        modifier.Reset();
                        int channel = firstTagChannel;

                        // Iterate observables for current result item.
                        foreach (var obs in tagObs)
                        {
                            // We evaluate the observable here and write obsValue
                            // to all grid positions below, UNLESS it's distance.
                            bool encodeDistance = obs.Evaluate(
                                item.Detectable, out float obsValue)
                                 == ObservableType.Distance;

                            if (!modifier.HasGridPositions)
                            {
                                if (item.HasPoints)
                                {
                                    // Is first observable we have any points for.

                                    foreach (var point in item.NormPoints)
                                    {
                                        // Normalized item point -> grid position.
                                        Vector2Int pos = m_GridBuffer.NormalizedToGridPos(point);

                                        if (encodeDistance)
                                        {
                                            // Ignore obsValue (is 0 for distance).

                                            // (Weighted) inverse distance, 1 (near) - 0 (far).
                                            float proximity = m_Normalization.Evaluate(point.z);

                                            // Override if closer.
                                            if (proximity > m_GridBuffer.Read(channel, pos))
                                            {
                                                // Write unmodified position to buffer.
                                                m_GridBuffer.Write(channel, pos, proximity);
                                                // Write unmodified position to modifier.
                                                modifier.AddPosition(pos, 1 - point.z); // unweighted
                                            }
                                            // else: ignore occluded.
                                        }
                                        else
                                        {
                                            // Write unmodified position to buffer.
                                            m_GridBuffer.Write(channel, pos, obsValue);

                                            // Write unmodified position to modifier.
                                            // NOTE Need some proximity value for PointModDilation.
                                            // Passing 0.5 to get a visible result, 0 does nothing.
                                            modifier.AddPosition(pos, 0.5f);
                                        }
                                    }

                                    // Will write additional positions to buffer.
                                    // NOTE So far, all point modifiers are only ADDING positions,
                                    // but that might not be the case for future modifiers.
                                    modifier.Process(m_GridBuffer, channel);

                                    if (m_Debug_IsEnabled)
                                    {
                                        // Store modified grid positions.
                                        m_Debug_ChannelData.AddGridPositions(channel, modifier.GridPositions);
                                    }
                                }
                            }
                            else
                            {
                                // Is additional observable.
                                //
                                // The modifier already contains the required grid positions,
                                // even if we haven't applied any modifications (PointModNone).
                                //
                                // The reason for requiring distance as the first observable,
                                // is that we write obsValue to ALL stored grid positions here.
                                // Distance is the only observable with individual position values.
                                //
                                // Will write all positions to buffer.
                                modifier.Write(m_GridBuffer, channel, obsValue);

                                if (m_Debug_IsEnabled)
                                {
                                    // Store modified grid positions.
                                    m_Debug_ChannelData.AddGridPositions(channel, modifier.GridPositions);
                                }
                            }

                            // Next observable...
                            channel++;
                        }
                    }
                }

                firstTagChannel += tagObs.Count();
            }
        }
    }
}