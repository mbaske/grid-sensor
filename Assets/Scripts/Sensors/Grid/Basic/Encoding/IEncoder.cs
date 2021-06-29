
namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Interface for encoders.
    /// An encoder is responsible for writing <see cref="DetectionResult"/> contents
    /// to the <see cref="Grid.GridBuffer"/> which is used by the <see cref="GridSensor"/>.
    /// </summary>
    public interface IEncoder
    {
        /// <summary>
        /// <see cref="IEncodingSettings"/> to use for encoding.
        /// </summary>
        IEncodingSettings Settings { get;  set; }

        /// <summary>
        /// The <see cref="Grid.GridBuffer"/> to write to.
        /// </summary>
        GridBuffer GridBuffer { set; }

        /// <summary>
        /// Encodes a <see cref="DetectionResult"/>.
        /// </summary>
        /// <param name="result"><see cref="DetectionResult"/> to encode</param>
        void Encode(DetectionResult result);
    }
}