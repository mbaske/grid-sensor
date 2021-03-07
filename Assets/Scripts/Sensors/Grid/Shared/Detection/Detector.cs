
namespace MBaske.Sensors.Grid
{
    public abstract class Detector
    {
        public DetectionResult Result { get; protected set; }
        public abstract DetectionResult Update();
        public abstract void Reset();
    }
}
