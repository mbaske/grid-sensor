
namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Interface for detectable object.
    /// </summary>
    public interface IDetectable
    {
        /// <summary>
        /// The object's tag.
        /// </summary>
        string Tag { get; }

        /// <summary>
        /// The object's name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The object's <see cref="ObservableCollection"/>.
        /// </summary>
        ObservableCollection Observables { get; }

        /// <summary>
        /// Creates an <see cref="ObservableCollection"/> instance.
        /// </summary>
        ObservableCollection InitObservables();

        /// <summary>
        /// Adds getter method references to <see cref="ObservableCollection"/> instance.
        /// </summary>
        void AddObservables();
    }
}