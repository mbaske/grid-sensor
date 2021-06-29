using System;
using System.Collections;
using UnityEngine;

namespace MBaske
{
    /// <summary>
    /// Poolable iten.
    /// </summary>
    public class Poolable : MonoBehaviour
    {
        /// <summary>
        /// Invoked OnDiscard.
        /// </summary>
        public event Action<Poolable> DiscardEvent;

        /// <summary>
        /// Item's group index in pool.
        /// </summary>
        public int GroupIndex { get; set; }

        [SerializeField]
        protected float m_Lifetime = -1;

        private IEnumerator m_Discard;

        /// <summary>
        /// Performs actions immediately after spawning.
        /// </summary>
        public virtual void OnSpawn()
        {
            if (m_Lifetime > 0)
            {
                DiscardAfter(m_Lifetime);
            }
        }

        /// <summary>
        /// Discards item (pool recycle).
        /// </summary>
        public void Discard()
        {
            DiscardAfter(0);
        }

        /// <summary>
        /// Discards item delayed (pool recycle).
        /// </summary>
        /// <param name="secs">Seconds until discard</param>
        public void DiscardAfter(float secs)
        {
            if (m_Discard != null)
            {
                StopCoroutine(m_Discard);
            }

            if (secs > 0)
            {
                m_Discard = DiscardAfterSecs(secs);
                StartCoroutine(m_Discard);
            }
            else
            {
                OnDiscard();
            }
        }

        protected virtual void OnDiscard()
        {
            DiscardEvent.Invoke(this);
        }

        private IEnumerator DiscardAfterSecs(float secs = 0)
        {
            yield return new WaitForSeconds(secs);
            OnDiscard();
        }
    }
}