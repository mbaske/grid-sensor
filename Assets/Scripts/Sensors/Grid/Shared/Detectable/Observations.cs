using System;
using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    public class Observations
    {
        public bool IsEmpty => m_Names == null;

        private List<string> m_Names;
        private List<Func<float>> m_Funcs;

        public int Add(Func<float> func, string name = "")
        {
            m_Names ??= new List<string>();
            m_Funcs ??= new List<Func<float>>();

            name = name.Length == 0 ? "Value " + m_Funcs.Count : name;

            if (m_Names.Contains(name))
            {
                Debug.LogError($"Name '{name}' already added");
            }
            else if (m_Funcs.Contains(func))
            {
                Debug.LogError($"Method {func} already added");
            }
            else
            {
                m_Names.Add(name);
                m_Funcs.Add(func);
            }

            return m_Funcs.Count;
        }

        public float Evaluate(int index)
        {
            return Mathf.Clamp01(m_Funcs[index].Invoke());
        }

        public int GetIndex(string name)
        {
            return m_Names.IndexOf(name);
        }

        public bool HasObservations(out IList<string> names)
        {
            if (!IsEmpty)
            {
                names = m_Names;
                return true;
            }

            names = null;
            return false;
        }
    }
}