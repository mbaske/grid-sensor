using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Wrapper for <see cref="Observable"/> list. 
    /// </summary>
    [Serializable]
    public class ObservableCollection
    {
        public int Count => m_Observables.Count;

        [SerializeField, HideInInspector]
        private List<Observable> m_Observables;

        /// <summary>
        /// Creates a <see cref="ObservableCollection"/> instance.
        /// </summary>
        public ObservableCollection()
        {
            m_Observables = new List<Observable>();
        }

        /// <summary>
        /// Returns deep copy of this instance.
        /// </summary>
        /// <returns><see cref="ObservableCollection"/> copy</returns>
        public ObservableCollection Copy()
        {
            var copy = new ObservableCollection();
            foreach (var obs in m_Observables)
            {
                copy.Add(obs.Copy());
            }
            return copy;
        }

        /// <summary>
        /// Copies <see cref="Observable"/> list to target list.
        /// </summary>
        /// <param name="target">The target list</param>
        public void CopyTo(List<Observable> target)
        {
            target.Clear();
            target.AddRange(m_Observables);
        }

        /// <summary>
        /// Returns the <see cref="Observable"/> instance at a specified index.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns><see cref="Observable"/> instance</returns>
        public Observable GetObservable(int index)
        {
            return m_Observables[index];
        }

        /// <summary>
        /// Whether this instance contains any <see cref="Observable"/>s NOT 
        /// included in a specified list.
        /// </summary>
        /// <param name="list"><see cref="Observable"/> list to check against</param>
        /// <param name="result">List of additional <see cref="Observable"/>s (output)</param>
        /// <returns>True if additional <see cref="Observable"/>s were found</returns>
        public bool ContainsOtherThan(IList<Observable> list, out IList<Observable> result)
        {
            result = new List<Observable>();

            foreach (var obs in m_Observables)
            {
                if (!list.Any(o => o.Name == obs.Name))
                {
                    result.Add(obs);
                }
            }

            return result.Count > 0;
        }

        /// <summary>
        /// Checks for length and value equality.
        /// </summary>
        /// <param name="other"><see cref="ObservableCollection"/> to check against</param>
        /// <returns>True if equal</returns>
        public bool Equals(ObservableCollection other)
        {
            if (other == null || other.Count != Count)
            {
                return false;
            }

            for (int i = 0, n = Count; i < n; i++)
            {
                if (!other.GetObservable(i).Equals(m_Observables[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Adds a new <see cref="Observable"/> to the list.
        /// </summary>
        /// <param name="name"><see cref="Observable"/>'s name</param>
        /// <param name="getter">Getter method reference</param>
        /// <returns>Updated length of <see cref="Observable"/> list</returns>
        public int Add(string name, Func<float> getter)
        {
            if (name == Observable.Distance || name == Observable.OneHot)
            {
                Debug.LogError($"'{name}' is a dedicated observable name.");
            }
            if (m_Observables.Any(o => o.Name == name))
            {
                Debug.LogError($"Observable name '{name}' already added.");
            }
            else if (m_Observables.Any(o => o.Getter == getter))
            {
                Debug.LogError($"Observable getter {getter} already added.");
            }
            else
            {
                m_Observables.Add(new Observable(ObservableType.User, name, m_Observables.Count, getter));
            }

            return m_Observables.Count;
        }

        /// <summary>
        /// Adds an <see cref="Observable"/> to the list.
        /// </summary>
        /// <param name="observable"><see cref="Observable"/> to add</param>
        public void Add(Observable observable)
        {
            m_Observables.Add(observable);
        }
    }
}