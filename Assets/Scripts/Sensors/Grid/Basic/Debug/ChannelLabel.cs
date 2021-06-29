using UnityEngine;
using System;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Stores a name and color usually associated with an observable.
    /// </summary>
    [Serializable]
    public struct ChannelLabel
    {
        /// <summary>
        /// Name/Label.
        /// </summary>
        [Tooltip("Name used for debugging / visualization.")]
        public string Name;

        /// <summary>
        /// Debug Color.
        /// </summary>
        [Tooltip("Color used for debugging / visualization.")]
        public Color Color;

        public ChannelLabel(string name, Color color)
        {
            Name = name;
            Color = color;
        }

        /// <summary>
        /// Factory for default <see cref="ChannelLabel"/>.
        /// </summary>
        public static ChannelLabel Default
            => new ChannelLabel()
            {
                Name = "Observable",
                Color = Color.cyan
            };
    }
}