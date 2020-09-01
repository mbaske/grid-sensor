using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MBaske
{
    /// <summary>
    /// 3D data structure for storing float values and pixel colors.
    /// </summary>
    public class PixelGrid : ChannelGrid
    {
        protected int m_NumLayers;

        /// <summary>
        /// The number of layers in the grid. A layer contains three (color) channels.
        /// Equivalent to the number of textures encoded by the <see cref="GridSensor"/>.
        /// </summary>
        public int NumLayers
        {
            get { return m_NumLayers; }
        }

        protected Color32[][] m_Colors;

        /// <summary>
        /// Initializes the grid.
        /// </summary>
        /// <param name="numChannels">The number of channels.</param>
        /// <param name="width">The width of the grid.</param>
        /// <param name="height">The height of the grid.</param>
        /// <param name="name">The name of the grid instance.</param>
        public PixelGrid(int numChannels, int width, int height, string name = "PixelGrid")
            : base(numChannels, width, height, name) { }

        protected override void Initialize()
        {
            base.Initialize();

            m_NumLayers = Mathf.CeilToInt(m_NumChannels / 3f);
            m_Colors = new Color32[m_NumLayers][];
            for (int i = 0; i < m_NumLayers; i++)
            {
                m_Colors[i] = new Color32[m_Width * m_Height];
            }
            ResetColors();
        }

        /// <summary>
        /// Clears all grid values by setting them to 0. Resets all pixels to black.
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            ResetColors();
        }

        /// <summary>
        /// Writes a float value to a specified grid cell and sets the corresponding pixel color.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <param name="x">The x position of the cell.</param>
        /// <param name="y">The y position of the cell.</param>
        /// <param name="value">The value to write.</param>
        public override void Write(int channel, int x, int y, float value)
        {
            Debug.Assert(value >= 0f && value <= 1f, "Value must be between 0 and 1");
            base.Write(channel, x, y, value);
            int layer = channel / 3;
            int color = channel - layer * 3;
            // Bottom to top, left to right.
            m_Colors[layer][(m_Height - y - 1) * m_Width + x][color] = (byte)(value * 255f);
        }

        /// <summary>
        /// Iterates the color values for the grid layers.
        /// </summary>
        /// <returns>Color32 array for each layer.</returns>
        public IEnumerable<Color32[]> GetColors()
        {
            for (int i = 0; i < m_NumLayers; i++)
            {
                yield return m_Colors[i];
            }
        }

        private void ResetColors()
        {
            var black = Enumerable.Repeat(new Color32(0, 0, 0, 255), m_Width * m_Height).ToArray();
            for (int i = 0; i < m_NumLayers; i++)
            {
                System.Array.Copy(black, m_Colors[i], m_Colors[i].Length);
            }
        }
    }
}
