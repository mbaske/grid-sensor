using UnityEngine;
using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    public enum ShapeModifierType
    {
        None, Dilation, Downsampling, OrthogonalFill, DiagonalFill
    }

    public abstract class ShapeModifier
    {
        public static IDictionary<string, ShapeModifier> CreateModifiers(
            IEncodingSettings settings, ChannelGrid grid)
        {
            var modifiers = new Dictionary<string, ShapeModifier>();

            foreach (string tag in settings.DetectableTags)
            {
                ShapeModifier modifier = settings.GetShapeModifierType(tag) switch
                {
                    ShapeModifierType.Dilation => new ShapeModDilation(),
                    ShapeModifierType.Downsampling => new ShapeModDownsampling(),
                    ShapeModifierType.OrthogonalFill => new ShapeModOrthogonalFill(),
                    ShapeModifierType.DiagonalFill => new ShapeModDiagonalFill(),
                    _ => new ShapeModNone(),
                };
                modifier.Initialize(grid);
                modifiers.Add(tag, modifier);
            }

            return modifiers;
        }


        protected int m_xMin;
        protected int m_xMax;
        protected int m_yMin;
        protected int m_yMax;
        protected int Width => m_xMax - m_xMin + 1;
        protected int Height => m_yMax - m_yMin + 1;

        protected HashSet<Vector2Int> m_UniqueGridPoints;

        public virtual void Initialize(ChannelGrid grid)
        {
            // TODO Capacity? https://stackoverflow.com/a/23071206
            m_UniqueGridPoints = new HashSet<Vector2Int>();
        }

        public virtual void Clear()
        {
            m_UniqueGridPoints.Clear();
            m_xMin = 9999;
            m_xMax = 0;
            m_yMin = 9999;
            m_yMax = 0;
        }

        public virtual void AddPoint(Vector2Int p, float z = 0)
        {
            m_UniqueGridPoints.Add(p);
            Expand(p.x, p.y);
        }

        public virtual void Process(ChannelGrid grid, int channel) { }

        // Write additional observations besides distance (3D) or first 
        // observation (2D), using either the originally added points or
        // the points calculated in Process().

        public virtual void Write(ChannelGrid grid, int channel, float value)
        {
            foreach (Vector2Int p in m_UniqueGridPoints)
            {
                grid.Write(channel, p, value);
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