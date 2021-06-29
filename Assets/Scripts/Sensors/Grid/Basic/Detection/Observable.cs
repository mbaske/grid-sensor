using System;
using UnityEngine;
using NaughtyAttributes;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// The type of <see cref="Observable"/>.
    /// </summary>
    public enum ObservableType
    {
        Distance, OneHot, User
    };

    /// <summary>
    /// Stores references to getter methods for custom observable values.
    /// Each <see cref="Observable"/> requires one sensor observation channel.
    /// </summary>
    [Serializable]
    public class Observable
    {
        // Dedicated observable names.
        public const string Distance = "Distance";
        public const string OneHot = "One-Hot";

        /// <summary>
        /// <see cref="Observable"/>'s name.
        /// Although marked as hidden, it is displayed as the list item name.
        /// </summary>
        [HideInInspector]
        public string Name;

        /// <summary>
        /// <see cref="Observable"/>'s type.
        /// </summary>
        [HideInInspector]
        public ObservableType Type;

        /// <summary>
        /// <see cref="Observable"/>'s index. Refers to user defined observable.
        /// </summary>
        [HideInInspector]
        public int Index;

        /// <summary>
        /// <see cref="Observable"/>'s getter method, typically defined in a class
        /// that inherits from <see cref="DetectableGameObject"/>.
        /// </summary>
        [HideInInspector]
        public Func<float> Getter;

        /// <summary>
        /// Whether this <see cref="Observable"/> is included in sensor observations.
        /// </summary>
        [Tooltip("Whether this observable is included in sensor observations.")]
        public bool Enabled = true;

        /// <summary>
        /// Color value for inspector debug view.
        /// </summary>
        [Label("Debug"), AllowNesting]
        [Tooltip("Color value for inspector debug view.")]
        public Color Color;

        public Observable(
            ObservableType type, 
            string name,
            int index = -1, // default for dedicated types
            Func<float> getter = null)
        {
            Type = type;
            Name = name;
            Index = index;
            Getter = getter;
            // TODO Should be some systematic color scheme.
            Color = UnityEngine.Random.ColorHSV(0, 1, 0.5f, 1, 0.5f, 1, 1, 1);
        }

        /// <summary>
        /// Returns the <see cref="Observable"/>'s current value. 
        /// Invokes getter on associated <see cref="IDetectable"/>
        /// and clamps the return value to 0 / +1.
        /// </summary>
        /// <returns><see cref="Observable"/>'s value</returns>
        public float Value()
        {
            return Mathf.Clamp01(Getter.Invoke());
        }

        /// <summary>
        /// Returns the <see cref="ObservableType"/>.
        /// Out value depends on <see cref="ObservableType"/>:
        /// - User: evaluates the specified <see cref="IDetectable"/>'s Observables[Index].
        /// - Distance: output = 0, encoder handles distance values.
        /// - One-Hot: output = 1.
        /// </summary>
        /// <param name="detectable"><see cref="IDetectable"/> to evaluate</param>
        /// <param name="value">Observed value (output)</param>
        /// <returns><see cref="ObservableType"/></returns>
        public ObservableType Evaluate(IDetectable detectable, out float value)
        {
            value = Type switch
            {
                // NOTE We always refer to the detectable's original observable 
                // instance when invoking the getter method, using the observable's
                // index. This is why a copy of that observable doesn't contain a 
                // getter itself. Copies are only used for organizing observables 
                // for the encoding settings.
                ObservableType.User 
                  => detectable.Observables.GetObservable(Index).Value(),
                ObservableType.OneHot 
                  => 1,
                _ => 0
            };

            return Type;
        }

        /// <summary>
        /// Copies <see cref="Observable"/> instance.
        /// A copy needs to contain the original's type, name and index.
        /// It doesn't have a getter reference, since evaluation is performed on a 
        /// specific <see cref="IDetectable"/> instance, see <see cref="Evaluate"/>.
        /// </summary>
        /// <returns>Observable copy</returns>
        public Observable Copy()
        {
            return new Observable(Type, Name, Index);
        }

        /// <summary>
        /// Checks for value equality.
        /// </summary>
        /// <param name="other"><see cref="Observable"/> to check against</param>
        /// <returns>True if equal</returns>
        public bool Equals(Observable other)
        {
            return other.Type == Type && other.Name == Name;
        }
    }
}