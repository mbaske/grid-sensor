using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Abstract base class for sensor constraints.
    /// </summary>
    public abstract class DetectionConstraint
    {
        /// <summary>
        /// Whether the <see cref="DetectionConstraint"/> contains a local point.
        /// </summary>
        /// <param name="localPoint">Point in sensor's local space</param>
        /// <param name="normPoint">Normalized point (output)</param>
        /// <returns>True if the <see cref="DetectionConstraint"/> contains the point</returns>
        public abstract bool ContainsPoint(Vector3 localPoint, out Vector3 normPoint);

        /// <summary>
        /// Returns the normalized distance between sensor component and point.
        /// Used for 3D detection only.
        /// </summary>
        /// <param name="localPoint">Point in sensor's local space</param>
        /// <returns>Normalized distance</returns>
        public virtual float GetNormalizedDistance(Vector3 localPoint) => 0;
    }
}
