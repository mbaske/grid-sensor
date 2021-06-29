using UnityEngine;
using System.Collections.Generic;
using System;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Class that contains debug information for each observation channel.
    /// </summary>
    public class DebugChannelData : IDisposable
    {
        /// <summary>
        /// Factory. Creates a <see cref="DebugChannelData"/> instance 
        /// from specified <see cref="IEncodingSettings"/>.
        /// </summary>
        /// <param name="settings"><see cref="IEncodingSettings"/></param>
        /// <param name="storePositions">Whether to store grid positions</param>
        /// <returns>New <see cref="DebugChannelData"/> instance</returns>
        public static DebugChannelData FromSettings(
            IEncodingSettings settings, bool storePositions = true)
        {
            return new DebugChannelData(settings, storePositions);
        }

        /// <summary>
        /// Factory. Creates a <see cref="DebugChannelData"/> instance 
        /// from a specified <see cref="ChannelLabel"/> list.
        /// </summary>
        /// <param name="labels"><see cref="ChannelLabel"/> list</param>
        /// <param name="storePositions">Whether to store grid positions</param>
        /// <returns>New <see cref="DebugChannelData"/> instance</returns>
        public static DebugChannelData FromLabels(
            IList<ChannelLabel> labels, bool storePositions = false)
        {
            return new DebugChannelData(labels, storePositions);
        }

        /// <summary>
        /// Whether the debug info contains grid positions.
        /// If it doesn't, the <see cref="GridBufferDrawer>"/>
        /// needs to draw the entire <see cref="GridBuffer"/>.
        /// </summary>
        public bool HasGridPositions { get; private set; }
        // Occupied grid positions per channel.
        private readonly IList<HashSet<Vector2Int>> m_GridPositions;

        private readonly bool m_HasObservables;
        // Observables by channel. Stores references to get current 
        // debug colors without the need for re-initializing the sensor.
        private readonly IList<Observable> m_Observables;
        
        private readonly IList<ChannelLabel> m_Labels;

        /// <summary>
        /// Creates a <see cref="DebugChannelData"/> instance 
        /// from specified <see cref="IEncodingSettings"/>.
        /// </summary>
        /// <param name="settings"><see cref="IEncodingSettings"/></param>
        /// <param name="storePositions">Whether to store grid positions</param>
        private DebugChannelData(IEncodingSettings settings, bool storePositions)
        {
            m_Labels = new List<ChannelLabel>();
            m_Observables = new List<Observable>();
            m_HasObservables = true;
            
            if (storePositions)
            {
                HasGridPositions = true;
                m_GridPositions = new List<HashSet<Vector2Int>>();
            }

            var tags = settings.DetectableTags;

            foreach (string tag in tags)
            {
                var observables = settings.GetObservables(tag);

                foreach (var obs in observables)
                {
                    m_Observables.Add(obs);

                    m_Labels.Add(new ChannelLabel()
                    {
                        Name = $"{tag} / {obs.Name}",
                        Color = obs.Color
                    });

                    if (storePositions)
                    {
                        m_GridPositions.Add(new HashSet<Vector2Int>());
                    }
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="DebugChannelData"/> instance 
        /// from a specified <see cref="ChannelLabel"/> list.
        /// </summary>
        /// <param name="labels">Tag list</param>
        /// <param name="storePositions">Whether to store grid positions</param>
        private DebugChannelData(IList<ChannelLabel> labels, bool storePositions)
        {
            m_Labels = new List<ChannelLabel>(labels);
            int n = labels.Count;

            if (storePositions)
            {
                HasGridPositions = true;
                m_GridPositions = new List<HashSet<Vector2Int>>(n);

                for (int i = 0; i < n; i++)
                {
                    m_GridPositions.Add(new HashSet<Vector2Int>());
                }
            }
        }

        /// <summary>
        /// Removes the stored grid positions.
        /// </summary>
        public void ClearGridPositions()
        {
            foreach (var positions in m_GridPositions)
            {
                positions.Clear();
            }
        }

        /// <summary>
        /// Adds grid positions for specified channel.
        /// </summary>
        /// <param name="channel">Grid channel index</param>
        /// <param name="positions">Positions to add</param>
        public void AddGridPositions(int channel, HashSet<Vector2Int> positions)
        {
            m_GridPositions[channel].UnionWith(positions);
        }

        /// <summary>
        /// Adds grid a position for specified channel.
        /// </summary>
        /// <param name="channel">Grid channel index</param>
        /// <param name="positions">Positions to add</param>
        public void AddGridPosition(int channel, Vector2Int position)
        {
            m_GridPositions[channel].Add(position);
        }

        /// <summary>
        /// Returns the grid positions for a specified channel.
        /// </summary>
        /// <param name="channel">Grid channel index</param>
        /// <returns>Grid positions</returns>
        public HashSet<Vector2Int> GetGridPositions(int channel)
        {
            return m_GridPositions[channel];
        }

        /// <summary>
        /// Returns the <see cref="ChannelLabel.Name"/> for a specified channel.
        /// </summary>
        /// <param name="channel">Grid channel index</param>
        /// <returns>Label text</returns>
        public string GetChannelName(int channel)
        {
            return m_Labels[channel].Name;
        }

        /// <summary>
        /// Returns the debug color for a specified channel.
        /// The color is either stored in the channel's associated
        /// <see cref="ChannelLabel.Color"/> or retrieved from an 
        /// <see cref="Observable"/>, which allows for runtime
        /// inspector updates without sensor re-initialization.
        /// </summary>
        /// <param name="channel">Grid channel index</param>
        /// <returns>Color</returns>
        public Color GetColor(int channel)
        {
            return m_HasObservables
                ? m_Observables[channel].Color
                : m_Labels[channel].Color;
        }

        /// <summary>
        /// Cleans up internal data.
        /// </summary>
        public void Dispose()
        {
            if (HasGridPositions)
            {
                ClearGridPositions();
            }
            
            if (m_HasObservables)
            {
                m_Observables.Clear();
            }
            
            m_Labels.Clear();
        }
    }
}