using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Interface for encoding settings.
    /// </summary>
    public interface IEncodingSettings
    {
        /// <summary>
        /// List of detectable tags recognized by the <see cref="IEncoder"/>.
        /// </summary>
        IList<string> DetectableTags { get; }

        /// <summary>
        /// Returns the <see cref="PointModifierType"/> associated with a specific tag.
        /// </summary>
        /// <param name="tag">The specified tag</param>
        /// <returns>The <see cref="PointModifierType"/> for the tag</returns>
        PointModifierType GetPointModifierType(string tag);

        /// <summary>
        /// Enumerates <see cref="Observable"/>s for a specified tag.
        /// </summary>
        /// <param name="tag">The specified tag</param>
        /// <returns><see cref="Observable"/>s enumeration</returns>
        IEnumerable<Observable> GetObservables(string tag);
    }
}