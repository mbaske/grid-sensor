using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    public interface IEncodingSettings
    {
        IList<string> DetectableTags { get; }
        ShapeModifierType GetShapeModifierType(string tag);
        bool HasObservations(string tag, out IList<int> indices);
    }
}