using UnityEngine;
using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// The method used for modifying grid positions.
    /// </summary>
    public enum PointModifierType
    {
        None, OrthogonalFill, DiagonalFill, Dilation, Downsampling
    }

    /// <summary>
    /// Abstract base class for <see cref="PointModifier"/>s.
    /// Object shapes are represented as points. A <see cref="PointModifier"/> 
    /// fills the space between 2D points in order to render objects more 
    /// like solid objects.
    /// </summary>
    public abstract class PointModifier
    {
        /// <summary>
        /// <see cref="PointModifier"/> factory.
        /// </summary>
        /// <param name="settings"><see cref="IEncodingSettings"/></param>
        /// <param name="buffer"><see cref="GridBuffer"/> used by the sensor</param>
        /// <returns>Dictionary of <see cref="PointModifiers"/>s by tag</returns>
        public static IDictionary<string, PointModifier> CreateModifiers(
            IEncodingSettings settings, GridBuffer buffer)
        {
            var modifiers = new Dictionary<string, PointModifier>();

            foreach (string tag in settings.DetectableTags)
            {
                PointModifier modifier = settings.GetPointModifierType(tag) switch
                {
                    PointModifierType.Dilation => new PointModDilation(),
                    PointModifierType.Downsampling => new PointModDownsampling(),
                    PointModifierType.OrthogonalFill => new PointModOrthogonalFill(),
                    PointModifierType.DiagonalFill => new PointModDiagonalFill(),
                    _ => new PointModNone(),
                };
                modifier.Initialize(buffer);
                modifiers.Add(tag, modifier);
            }

            return modifiers;
        }



        /// <summary>
        /// Grid positions the <see cref="IEncoder"/> writes values to.
        /// </summary>
        public HashSet<Vector2Int> GridPositions { get; private set; }

        /// <summary>
        /// Whether any grid positions are stored.
        /// </summary>
        public bool HasGridPositions => GridPositions.Count > 0;

        protected int m_xMin;
        protected int m_xMax;
        protected int m_yMin;
        protected int m_yMax;

        // Min/Max inclusive.
        protected int Width => m_xMax - m_xMin + 1;
        protected int Height => m_yMax - m_yMin + 1;


        /// <summary>
        /// Initializes the <see cref="PointModifier"/>.
        /// </summary>
        /// <param name="buffer"><see cref="GridBuffer"/> used by the sensor</param>
        public virtual void Initialize(GridBuffer buffer)
        {
            // TODO Capacity? https://stackoverflow.com/a/23071206
            GridPositions = new HashSet<Vector2Int>();
        }

        /// <summary>
        /// Resets the <see cref="PointModifier"/>.
        /// </summary>
        public virtual void Reset()
        {
            GridPositions.Clear();

            m_xMin = 9999;
            m_xMax = 0;
            m_yMin = 9999;
            m_yMax = 0;
        }

        /// <summary>
        /// Adds a position to the <see cref="PointModifier"/>.
        /// </summary>
        /// <param name="pos">2D grid position</param>
        /// <param name="proximity">Inverse distance value, used by point dilation</param>
        public virtual void AddPosition(Vector2Int pos, float proximity = 0)
        {
            GridPositions.Add(pos);
            Expand(pos.x, pos.y);
        }

        /// <summary>
        /// Processes a specific channel of the <see cref="GridBuffer"/>.
        /// </summary>
        /// <param name="buffer"><see cref="GridBuffer"/> used by the sensor</param>
        /// <param name="channel">Grid channel index</param>
        public virtual void Process(GridBuffer buffer, int channel) { }


        /// <summary>
        /// Writes additional value to stored grid positions,
        /// using either the originally added positions 
        /// or the positions calculated in <see cref="Process"/>.
        /// </summary>
        /// <param name="buffer"><see cref="GridBuffer"/> used by the sensor</param>
        /// <param name="channel">Grid channel index</param>
        /// <param name="value">The value to write</param>
        public virtual void Write(GridBuffer buffer, int channel, float value)
        {
            foreach (Vector2Int pos in GridPositions)
            {
                buffer.Write(channel, pos, value);
            }
        }

        protected void Expand(int x, int y)
        {
            m_xMin = Mathf.Min(m_xMin, x);
            m_xMax = Mathf.Max(m_xMax, x);
            m_yMin = Mathf.Min(m_yMin, y);
            m_yMax = Mathf.Max(m_yMax, y);
        }
    }
}