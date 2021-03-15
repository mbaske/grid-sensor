using System.Collections.Generic;
using UnityEngine;

namespace MBaske
{
    public abstract class Pool<T> : MonoBehaviour where T : Poolable
    {
        [SerializeField]
        private T[] m_Prefabs;
        [SerializeField]
        private List<int> m_Capacities = new List<int>();

        private Stack<T>[] m_Inactive;
        protected IList<T>[] m_Active;


        private void OnValidate()
        {
            if (m_Prefabs != null)
            {
                int nP = m_Prefabs.Length;
                int nC = m_Capacities.Count;

                if (nP > nC)
                {
                    for (int i = nC; i < nP; i++)
                    {
                        m_Capacities.Add(64);
                    }
                }
                else if (nC > nP)
                {
                    for (int i = nC; i > nP; i--)
                    {
                        m_Capacities.RemoveAt(m_Capacities.Count - 1);
                    }
                }
            }
        }

        private void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            int n = m_Prefabs.Length;
            m_Active = new IList<T>[n];
            m_Inactive = new Stack<T>[n];

            for (int i = 0; i < n; i++)
            {
                int c = m_Capacities[i];
                m_Active[i] = new List<T>(c);
                m_Inactive[i] = new Stack<T>(c);

                for (int j = 0; j < c; j++)
                {
                    m_Inactive[i].Push(NewInstance(i));
                }
            }
        }

        public T Spawn(Vector3 position, int index = 0)
        {
            T obj = Spawn(index);
            obj.transform.position = position;

            return obj;
        }

        public T Spawn(Vector3 position, Quaternion rotation, int index = 0)
        {
            T obj = Spawn(index);
            obj.transform.position = position;
            obj.transform.rotation = rotation;

            return obj;
        }

        public T Spawn(int index = 0)
        {
            T obj = m_Inactive[index].Count > 0
                ? m_Inactive[index].Pop()
                : NewInstance(index);

            obj.gameObject.SetActive(true);
            m_Active[index].Add(obj);
            obj.OnSpawn();

            return obj;
        }

        public void DiscardAll()
        {
            for (int i = 0; i < m_Active.Length; i++)
            {
                DiscardAll(i);
            }
        }

        public void DiscardAll(int index)
        {
            var tmp = new List<T>(m_Active[index]);
            foreach (var obj in tmp)
            {
                Discard(obj);
            }
        }

        public void Discard(T obj)
        {
            obj.Discard();
        }

        protected void OnDiscard(Poolable obj)
        {
            obj.gameObject.SetActive(false);
            m_Inactive[obj.Index].Push((T)obj);
            m_Active[obj.Index].Remove((T)obj);
        }

        private T NewInstance(int index)
        {
            T obj = Instantiate(m_Prefabs[index], transform);
            obj.gameObject.SetActive(false);
            obj.DiscardEvent += OnDiscard;
            obj.Index = index;
            return obj;
        }
    }
}