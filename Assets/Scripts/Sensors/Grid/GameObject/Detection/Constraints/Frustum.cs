using System;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Frustum, used for curved FOV / spherical detection.
    /// </summary>
    [Serializable]
    public struct Frustum
    {
        // A valid frustum has view angles <= MaxAngle.
        public const float MaxAngle = 87;

        // We'll pass a frustum instance to the detector,
        // regardless of whether it is valid. Its helper
        // camera simply won't be created for an invalid
        // frustum.
        public bool IsValid;

        public float Left;
        public float Right;
        public float Bottom;
        public float Top;
        public float Near;
        public float Far;

        public override string ToString()
        {
            return $"Frustum, valid: {IsValid}, left: {Left}, right: {Right}, "
                 + $"bottom: {Bottom}, top: {Top}, near: {Near}, far: {Far}";
        }
    }
}