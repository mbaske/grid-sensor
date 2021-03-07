
namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Interface for detectable objects.
    /// </summary>
    public interface IDetectable
    {
        string Tag { get; }
        Observations Observations { get; }
        Observations InitObservations();
        void AddObservations();
    }
}