using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// 3D data structure for storing float values.
    /// </summary>
    public class ChannelGrid
    {
        /// <summary>
        /// The grid instance name.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }
        protected string m_Name;

        /// <summary>
        /// The number of grid channels.
        /// </summary>
        public int Channels
        {
            get { return m_Channels; }
            set { m_Channels = value; Allocate(); }
        }
        protected int m_Channels;

        /// <summary>
        /// The width of the grid.
        /// </summary>
        public int Width
        {
            get { return m_Width; }
            set { m_Width = value; Allocate(); }
        }
        protected int m_Width;

        /// <summary>
        /// The height of the grid.
        /// </summary>
        public int Height
        {
            get { return m_Height; }
            set { m_Height = value; Allocate(); }
        }
        protected int m_Height;

        /// <summary>
        /// The width and height of the grid as Vector2Int.
        /// </summary>
        public Vector2Int Size
        {
            get { return new Vector2Int(m_Width, m_Height); }
            set { m_Width = value.x; m_Height = value.y ; Allocate(); }
        }

        /// <summary>
        /// The grid's observation shape.
        /// </summary
        public GridShape Shape
        {
            get { return m_Shape; }
            set { m_Shape = value; Allocate(); }
        }
        protected GridShape m_Shape;


        // [channel][y * width + x]
        protected float[][] m_Values;

        /// <summary>
        /// Initializes the grid.
        /// </summary>
        /// <param name="channels">The number of channels.</param>
        /// <param name="width">The width of the grid.</param>
        /// <param name="height">The height of the grid.</param>
        /// <param name="name">The name of the grid instance.</param>
        public ChannelGrid(int channels, int width, int height, string name = "Grid")
        {
            m_Shape = new GridShape(channels, width, height);
            m_Name = name;

            Allocate();
        }

        /// <summary>
        /// Initializes the grid.
        /// </summary>
        /// <param name="shape">The grid's observation shape.</param>
        /// <param name="name">The name of the grid instance.</param>
        public ChannelGrid(GridShape shape, string name = "Grid")
        {
            m_Shape = shape;
            m_Name = name;

            Allocate();
        }


        protected virtual void Allocate()
        {
            m_Channels = m_Shape.Channels;
            m_Width = m_Shape.Width;
            m_Height = m_Shape.Height;

            m_Values = new float[m_Channels][];

            for (int i = 0; i < m_Channels; i++)
            {
                m_Values[i] = new float[m_Width * m_Height];
            }
        }

        /// <summary>
        /// Clears all grid values by setting them to 0.
        /// </summary>
        public virtual void Clear()
        {
            ClearChannels(0, m_Channels);
        }

        /// <summary>
        /// Clears grid values of specified channels by setting them to 0.
        /// <param name="start">The first channel's index.</param>
        /// <param name="length">The number of channels to clear.</param>
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
        /// <param name="channel">The channel index.</param>
        /// </summary>
        public virtual void ClearChannel(int channel)
        {
            if (channel < Channels)
            {
                System.Array.Clear(m_Values[channel], 0, m_Values[channel].Length);
            }
        }

        /// <summary>
        /// Writes a float value to a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index.</param>
        /// <param name="x">The cell's x position.</param>
        /// <param name="y">The cell's y position.</param>
        /// <param name="value">The value to write.</param>
        public virtual void Write(int channel, int x, int y, float value)
        {
            m_Values[channel][y * m_Width + x] = value;
        }

        /// <summary>
        /// Writes a float value to a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index.</param>
        /// <param name="pos">The cell's x/y position.</param>
        /// <param name="value">The value to write.</param>
        public virtual void Write(int channel, Vector2Int pos, float value)
        {
            Write(channel, pos.x, pos.y, value);
        }

        /// <summary>
        /// Tries to write a float value to a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index.</param>
        /// <param name="x">The cell's x position.</param>
        /// <param name="y">The cell's y position.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>True if the specified cell exists, false otherwise.</returns>
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
        /// <param name="channel">The cell's channel index.</param>
        /// <param name="pos">The cell's x/y position.</param>
        /// <param name="value">The value to write.</param>
        /// <returns>True if the specified cell exists, false otherwise.</returns>
        public virtual bool TryWrite(int channel, Vector2Int pos, float value)
        {
            return TryWrite(channel, pos.x, pos.y, value);
        }

        /// <summary>
        /// Reads a float value from a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index.</param>
        /// <param name="x">The cell's x position.</param>
        /// <param name="y">The cell's y position.</param>
        /// <returns>Float value of the specified cell.</returns>
        public virtual float Read(int channel, int x, int y)
        {
            return m_Values[channel][y * m_Width + x];
        }

        /// <summary>
        /// Reads a float value from a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index.</param>
        /// <param name="pos">The cell's x/y position.</param>
        /// <returns>Float value of the specified cell.</returns>
        public virtual float Read(int channel, Vector2Int pos)
        {
            return Read(channel, pos.x, pos.y);
        }

        /// <summary>
        /// Tries to read a float value from a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index.</param>
        /// <param name="x">The cell's x position.</param>
        /// <param name="y">The cell's y position.</param>
        /// <param name="value">The value of the specified cell if it exists, 0 otherwise.</param>
        /// <returns>True if the specified cell exists, false otherwise.</returns>
        public virtual bool TryRead(int channel, int x, int y, out float value)
        {
            bool hasPosition = Contains(x, y);
            value = hasPosition ? Read(channel, x, y) : 0;
            return hasPosition;
        }

        /// <summary>
        /// Tries to read a float value from a specified grid cell.
        /// </summary>
        /// <param name="channel">The cell's channel index.</param>
        /// <param name="pos">The cell's x/y position.</param>
        /// <param name="value">The value of the specified cell if it exists, 0 otherwise.</param>
        /// <returns>True if the specified cell exists, false otherwise.</returns>
        public virtual bool TryRead(int channel, Vector2Int pos, out float value)
        {
            return TryRead(channel, pos.x, pos.y, out value);
        }

        /// <summary>
        /// Checks if a specified position exists in the grid.
        /// </summary>
        /// <param name="x">The x position.</param>
        /// <param name="y">The y position.</param>
        /// <returns>True if the specified position exists, false otherwise.</returns>
        public virtual bool Contains(int x, int y)
        {
            return x >= 0 && x < m_Width && y >= 0 && y < m_Height;
        }

        /// <summary>
        /// Checks if a specified position exists in the grid.
        /// </summary>
        /// <param name="pos">The x/y position.</param>
        /// <returns>True if the specified position exists, false otherwise.</returns>
        public virtual bool Contains(Vector2Int pos)
        {
            return Contains(pos.x, pos.y);
        }

        /// <summary>
        /// Calculates a grid position from a normalized Vector2.
        /// </summary>
        /// <param name="norm">The normalized vector.</param>
        /// <returns>The grid position.</returns>
        public Vector2Int NormalizedToGridPos(Vector2 norm)
        {
            return new Vector2Int(
                (int)(norm.x * m_Width), 
                (int)(norm.y * m_Height)
            );
        }

        /// <summary>
        /// Calculates a grid rectangle from a normalized Rect.
        /// </summary>
        /// <param name="norm">The normalized rectangle.</param>
        /// <returns>The grid rectangle.</returns>
        public RectInt NormalizedToGridRect(Rect norm)
        {
            return new RectInt(
                (int)(norm.xMin * m_Width), 
                (int)(norm.yMin * m_Height),
                (int)(norm.width * m_Width), 
                (int)(norm.height * m_Height)
            );
        }
    }
}
