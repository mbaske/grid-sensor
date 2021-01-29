using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MLGridSensor
{
    /// <summary>
    /// 3D data structure for storing float values and pixel colors.
    /// </summary>
    public class PixelGrid : ChannelGrid
    {
        /// <summary>
        /// The number of layers in the grid. A layer contains three (color) channels.
        /// Equivalent to the number of textures encoded by the <see cref="GridSensor"/>.
        /// </summary>
        public int Layers
        {
            get { return m_Layers; }
        }
        protected int m_Layers;

        protected Color32[][] m_Colors;
        private Color32[] c_Black;

        /// <summary>
        /// Initializes the grid.
        /// </summary>
        /// <param name="numChannels">The number of channels.</param>
        /// <param name="width">The width of the grid.</param>
        /// <param name="height">The height of the grid.</param>
        /// <param name="name">The name of the grid instance.</param>
        public PixelGrid(int numChannels, int width, int height, string name = "PixelGrid")
            : base(numChannels, width, height, name) { }

        /// <summary>
        /// Initializes the grid.
        /// </summary>
        /// <param name="shape">The grid's observation shape.</param>
        /// <param name="name">The name of the grid instance.</param>
        public PixelGrid(GridObservationShape shape, string name = "PixelGrid")
            : base(shape, name) { }


        protected override void Allocate()
        {
            base.Allocate();

            m_Layers = Mathf.CeilToInt(m_Channels / 3f);
            m_Colors = new Color32[m_Layers][];

            for (int i = 0; i < m_Layers; i++)
            {
                m_Colors[i] = new Color32[m_Width * m_Height];
            }

            c_Black = Enumerable.Repeat(new Color32(0, 0, 0, 255), m_Width * m_Height).ToArray();
            ClearColors();
        }


        /// <summary>
        /// Clears all grid values by setting them to 0. Sets all pixels to black.
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            ClearColors();
        }

        /// <summary>
        /// Clears grid values of a specified layer by setting them to 0. Sets layer pixels to black.
        /// <param name="layer">The layer index.</param>
        /// </summary>
        public virtual void ClearLayer(int layer)
        {
            base.ClearChannel(layer * 3);

            if (m_Channels > layer * 3 + 1)
            {
                base.ClearChannel(layer * 3 + 1);
            }

            if (m_Channels > layer * 3 + 2)
            {
                base.ClearChannel(layer * 3 + 2);
            }
            
            ClearLayerColors(layer);
        }

        /// <summary>
        /// Clears grid values of a specified channel by setting them to 0. Sets channel's pixels' color to 0.
        /// <param name="channel">The channel index.</param>
        /// </summary>
        public override void ClearChannel(int channel)
        {
            base.ClearChannel(channel);
            ClearChannelColors(channel);
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
            Debug.Assert(value >= 0f && value <= 1f, $"Value {value} must be between 0 and 1");
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
            for (int i = 0; i < m_Layers; i++)
            {
                yield return GetColors(i);
            }
        }

        public Color32[] GetColors(int layer)
        {
            return m_Colors[layer];
        }

        private void ClearColors()
        {
            for (int i = 0; i < m_Layers; i++)
            {
                ClearLayerColors(i);
            }
        }

        private void ClearLayerColors(int layer)
        {
            System.Array.Copy(c_Black, m_Colors[layer], m_Colors[layer].Length);
        }

        private void ClearChannelColors(int channel)
        {
            int layer = channel / 3;
            int color = channel - layer * 3;

            for (int i = 0; i < m_Colors[layer].Length; i++)
            {
                m_Colors[layer][i][color] = 0;
            }
        }
    }
}
