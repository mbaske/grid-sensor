
namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Interface foor detectable objects.
    /// </summary>
    public interface IDetectable
    {
        string Tag { get; }
        Observations Observations { get; }
        Observations InitObservations();
        void AddObservations();
    }
}