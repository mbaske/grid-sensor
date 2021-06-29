using UnityEngine;
using Unity.MLAgents;
using System;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// 3D data structure for storing float values.
    /// Dimensions: channels x width x height.
    /// </summary>
    public class GridBuffer
    {
        /// <summary>
        /// Grid shape.
        /// </summary>
        [Serializable]
        public struct Shape
        {
            /// <summary>
            /// The number of grid channels.
            /// </summary>
            public int NumChannels;

            /// <summary>
            /// The width of the grid.
            /// </summary>
            public int Width;

            /// <summary>
            /// The height of the grid.
            /// </summary>
            public int Height;

            /// <summary>
            /// The grid size as Vector2Int.
            /// </summary>
            public Vector2Int Size
            {
                get { return new Vector2Int(Width, Height); }
                set { Width = value.x; Height = value.y; }
            }

            /// <summary>
            /// Creates a <see cref="Shape"/> instance.
            /// </summary>
            /// <param name="numChannels">Number of grid channels</param>
            /// <param name="width">Grid width</param>
            /// <param name="height">Grid height</param>
            public Shape(int numChannels, int width, int height)
            {
                NumChannels = numChannels;
                Width = width;
                Height = height;
            }

            /// <summary>
            /// Creates a <see cref="Shape"/> instance.
            /// </summary>
            /// <param name="numChannels">Number of grid channels</param>
            /// <param name="size">Grid size</param>
            public Shape(int numChannels, Vector2Int size)
                : this(numChannels, size.x, size.y) { }

            /// <summary>
            /// Validates the <see cref="Shape"/>.
            /// </summary>
            public void Validate()
            {
                if (NumChannels < 1)
                {
                    throw new UnityAgentsException("Grid buffer has no channels.");
                }

                if (Width < 1)
                {
                    throw new UnityAgentsException("Invalid grid buffer width " + Width);
                }

                if (Height < 1)
                {
                    throw new UnityAgentsException("Invalid grid buffer height " + Height);
                }
            }

            public override string ToString()
            {
                return $"Grid {NumChannels} x {Width} x {Height}";
            }
        }

        /// <summary>
        /// Returns a new <see cref="Shape"/> instance.
        /// </summary>
        /// <returns>Grid shape</returns>
        public Shape GetShape()
        {
            return new Shape(m_NumChannels, m_Width, m_Height);
        }

        /// <summary>
        /// The number of grid channels.
        /// </summary>
        public int NumChannels
        {
            get { return m_NumChannels; }
            set { m_NumChannels = value; Initialize(); }
        }
        private int m_NumChannels;

        /// <summary>
        /// The width of the grid.
        /// </summary>
        public int Width
        {
            get { return m_Width; }
            set { m_Width = value; Initialize(); }
        }
        private int m_Width;

        /// <summary>
        /// The height of the grid.
        /// </summary>
        public int Height
        {
            get { return m_Height; }
            set { m_Height = value; Initialize(); }
        }
        private int m_Height;

        /// <summary>
        /// Whether the buffer was changed since last Clear() call.
        /// </summary>
        //public bool IsDirty { get; private set; }


        // [channel][y * width + x]
        private float[][] m_Values;

        /// <summary>
        /// Creates a <see cref="GridBuffer"/> instance.
        /// </summary>
        /// <param name="numChannels">Number of grid channels</param>
        /// <param name="width">Grid width</param>
        /// <param name="height">Grid height</param>
        public GridBuffer(int numChannels, int width, int height)
        {
            m_NumChannels = numChannels;
            m_Width = width;
            m_Height = height;

            Initialize();
        }

        /// <summary>
        /// Creates a <see cref="GridBuffer"/> instance.
        /// </summary>
        /// <param name="numChannels">Number of grid channels</param>
        /// <param name="size">Grid size</param>
        public GridBuffer(int numChannels, Vector2Int size)
            : this(numChannels, size.x, size.y) { }

        /// <summary>
        /// Creates a <see cref="GridBuffer"/> instance.
        /// </summary>
        /// <param name="shape"><see cref="Shape"/> of the grid</param>
        public GridBuffer(Shape shape)
            : this(shape.NumChannels, shape.Width, shape.Height) { }


        protected virtual void Initialize()
        {
            m_Values = new float[NumChannels][];

            for (int i = 0; i < NumChannels; i++)
            {
                m_Values[i] = new float[Width * Height];
            }
        }

        /// <summary>
        /// Clears all grid values by setting them to 0.
        /// </summary>
        public virtual void Clear()
        {
            ClearChannels(0, NumChannels);
            //IsDirty = false;
        }

        /// <summary>
        /// Clears grid values of specified channels by setting them to 0.
        /// <param name="start">The first channel's index</param>
        /// <param name="length">The number of channels to clear</param>
        /// </summary>
        public virtual void ClearChannels(int start, int length)
        {
            for (int i = 0; i < length; i++)
            {
                ClearChannel(start + i);
            }
        }

        /// <summary>
        /// Clears grid values of a specified channel by setting them to 0.
        /// <param name="channel">The channel index</param>
        /// </summary>
        public virtual void ClearChannel(int channel)
        {
            if (channel < NumChannels)
            {
                Array.Clear(m_Values[channel], 0, m_Values[channel].Length);
            }
        }

        /// <summary>
        /// Writes a float value to a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index</param>
        /// <param name="x">The cell's x position</param>
        /// <param name="y">The cell's y position</param>
        /// <param name="value">The value to write</param>
        public virtual void Write(int channel, int x, int y, float value)
        {
            m_Values[channel][y * Width + x] = value;
            //IsDirty = true;
        }

        /// <summary>
        /// Writes a float value to a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index</param>
        /// <param name="pos">The cell's x/y position</param>
        /// <param name="value">The value to write</param>
        public virtual void Write(int channel, Vector2Int pos, float value)
        {
            Write(channel, pos.x, pos.y, value);
        }

        /// <summary>
        /// Tries to write a float value to a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index</param>
        /// <param name="x">The cell's x position</param>
        /// <param name="y">The cell's y position</param>
        /// <param name="value">The value to write</param>
        /// <returns>True if the specified cell exists, false otherwise</returns>
        public virtual bool TryWrite(int channel, int x, int y, float value)
        {
            bool hasPosition = Contains(x, y);
            if (hasPosition)
            {
                Write(channel, x, y, value);
            }
            return hasPosition;
        }

        /// <summary>
        /// Tries to write a float value to a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index</param>
        /// <param name="pos">The cell's x/y position</param>
        /// <param name="value">The value to write</param>
        /// <returns>True if the specified cell exists, false otherwise</returns>
        public virtual bool TryWrite(int channel, Vector2Int pos, float value)
        {
            return TryWrite(channel, pos.x, pos.y, value);
        }

        /// <summary>
        /// Reads a float value from a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index</param>
        /// <param name="x">The cell's x position</param>
        /// <param name="y">The cell's y position</param>
        /// <returns>Float value of the specified cell</returns>
        public virtual float Read(int channel, int x, int y)
        {
            return m_Values[channel][y * Width + x];
        }

        /// <summary>
        /// Reads a float value from a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index</param>
        /// <param name="pos">The cell's x/y position</param>
        /// <returns>Float value of the specified cell</returns>
        public virtual float Read(int channel, Vector2Int pos)
        {
            return Read(channel, pos.x, pos.y);
        }

        /// <summary>
        /// Tries to read a float value from a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index</param>
        /// <param name="x">The cell's x position</param>
        /// <param name="y">The cell's y position</param>
        /// <param name="value">The value of the specified cell if it exists, 0 otherwise</param>
        /// <returns>True if the specified cell exists, false otherwise</returns>
        public virtual bool TryRead(int channel, int x, int y, out float value)
        {
            bool hasPosition = Contains(x, y);
            value = hasPosition ? Read(channel, x, y) : 0;
            return hasPosition;
        }

        /// <summary>
        /// Tries to read a float value from a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index</param>
        /// <param name="pos">The cell's x/y position</param>
        /// <param name="value">The value of the specified cell if it exists, 0 otherwise</param>
        /// <returns>True if the specified cell exists, false otherwise</returns>
        public virtual bool TryRead(int channel, Vector2Int pos, out float value)
        {
            return TryRead(channel, pos.x, pos.y, out value);
        }

        /// <summary>
        /// Checks if a specified position exists in the grid.
        /// </summary>
        /// <param name="x">The x position</param>
        /// <param name="y">The y position</param>
        /// <returns>True if the specified position exists, false otherwise</returns>
        public virtual bool Contains(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>
        /// Checks if a specified position exists in the grid.
        /// </summary>
        /// <param name="pos">The x/y position</param>
        /// <returns>True if the specified position exists, false otherwise</returns>
        public virtual bool Contains(Vector2Int pos)
        {
            return Contains(pos.x, pos.y);
        }

        /// <summary>
        /// Calculates a grid position from a normalized Vector2.
        /// </summary>
        /// <param name="norm">The normalized vector</param>
        /// <returns>The grid position</returns>
        public Vector2Int NormalizedToGridPos(Vector2 norm)
        {
            return new Vector2Int(
                (int)(norm.x * Width),
                (int)(norm.y * Height)
            );
        }

        /// <summary>
        /// Calculates a grid rectangle from a normalized Rect.
        /// </summary>
        /// <param name="norm">The normalized rectangle</param>
        /// <returns>The grid rectangle</returns>
        public RectInt NormalizedToGridRect(Rect norm)
        {
            return new RectInt(
                (int)(norm.xMin * Width),
                (int)(norm.yMin * Height),
                (int)(norm.width * Width),
                (int)(norm.height * Height)
            );
        }

        /// <summary>
        /// Returns the number of grid layers.
        /// Not supported by <see cref="GridBuffer"/> base class.
        /// </summary>
        /// <returns>Number of layers</returns>
        public virtual int GetNumLayers()
        {
            ThrowNotSupportedError();
            return 0;
        }

        /// <summary>
        /// Returns the grid layer colors.
        /// Not supported by <see cref="GridBuffer"/> base class.
        /// </summary>
        /// <returns>Color32 array [layerIndex][gridPosition]</returns>
        public virtual Color32[][] GetLayerColors()
        {
            ThrowNotSupportedError();
            return null;
        }

        private void ThrowNotSupportedError()
        {
            throw new UnityAgentsException(
                "GridBuffer doesn't support PNG compression. " +
                "Use the ColorGridBuffer instead.");
        }
    }
}
