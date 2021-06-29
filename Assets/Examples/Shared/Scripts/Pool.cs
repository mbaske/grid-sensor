using System.Collections.Generic;
using UnityEngine;

namespace MBaske
{
    /// <summary>
    /// Abstract generic pool.
    /// <see cref="Poolable"/> items can be organized
    /// into groups which are being referenced by index.
    /// </summary>
    /// <typeparam name="T"><see cref="Poolable"/>.</typeparam>
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

        /// <summary>
        /// Spawns item at position.
        /// </summary>
        /// <param name="position">Spawn position</param>
        /// <param name="groupIndex">Item group index</param>
        /// <returns><see cref="Poolable"/> item</returns>
        public T Spawn(Vector3 position, int groupIndex = 0)
        {
            T obj = Spawn(groupIndex);
            obj.transform.position = position;

            return obj;
        }

        /// <summary>
        /// Spawns item at position with rotation.
        /// </summary>
        /// <param name="position">Spawn position</param>
        /// <param name="rotation">Spawn rotation</param>
        /// <param name="groupIndex">Item group index</param>
        /// <returns><see cref="Poolable"/> item</returns>
        public T Spawn(Vector3 position, Quaternion rotation, int groupIndex = 0)
        {
            T obj = Spawn(groupIndex);
            obj.transform.position = position;
            obj.transform.rotation = rotation;

            return obj;
        }

        /// <summary>
        /// Spawns item.
        /// </summary>
        /// <param name="groupIndex">Item group index</param>
        /// <returns><see cref="Poolable"/> item</returns>
        public T Spawn(int groupIndex = 0)
        {
            T obj = m_Inactive[groupIndex].Count > 0
                ? m_Inactive[groupIndex].Pop()
                : NewInstance(groupIndex);

            obj.gameObject.SetActive(true);
            m_Active[groupIndex].Add(obj);
            obj.OnSpawn();

            return obj;
        }

        /// <summary>
        /// Discards all active items in all groups.
        /// </summary>
        public void DiscardAll()
        {
            for (int i = 0; i < m_Active.Length; i++)
            {
                DiscardAll(i);
            }
        }

        /// <summary>
        /// Discards all active items in specified group.
        /// </summary>
        /// /// <param name="groupIndex">Item group index</param>
        public void DiscardAll(int groupIndex)
        {
            var tmp = new List<T>(m_Active[groupIndex]);
            foreach (var obj in tmp)
            {
                Discard(obj);
            }
        }

        /// <summary>
        /// Discards specified item.
        /// </summary>
        /// <param name="obj"><see cref="Poolable"/> item to discard</param>
        public void Discard(T obj)
        {
            obj.Discard();
        }

        protected void OnDiscard(Poolable obj)
        {
            obj.gameObject.SetActive(false);
            m_Inactive[obj.GroupIndex].Push((T)obj);
            m_Active[obj.GroupIndex].Remove((T)obj);
        }

        private T NewInstance(int index)
        {
            T obj = Instantiate(m_Prefabs[index], transform);
            obj.gameObject.SetActive(false);
            obj.DiscardEvent += OnDiscard;
            obj.GroupIndex = index;
            return obj;
        }
    }
}