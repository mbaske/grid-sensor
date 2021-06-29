
namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Interface for detectors.
    /// A detector is responsible for generating a <see cref="DetectionResult"/>
    /// which can be encoded by an <see cref="IEncoder"/>.
    /// </summary>
    public interface IDetector
    {
        /// <summary>
        /// <see cref="DetectionResult"/>, generated in <see cref="OnSensorUpdate"/>.
        /// </summary>
        public DetectionResult Result { get; }

        /// <summary>
        /// Invokes detection logic when sensor is updated via ML-Agents framework.
        /// </summary>
        public void OnSensorUpdate();

        /// <summary>
        /// Invokes clean up etc. when sensor is reset at the end of each episode
        /// via ML-Agents framework.
        /// </summary>
        public void OnSensorReset();
    }
}
