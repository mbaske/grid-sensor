using System;
using System.Collections;
using UnityEngine;

namespace MBaske
{
    public abstract class Poolable : MonoBehaviour
    {
        public event Action<Poolable> DiscardEvent;

        public int Index { get; set; }

        [SerializeField]
        protected float m_Lifetime = -1;

        private IEnumerator m_Discard;

        public virtual void OnSpawn()
        {
            if (m_Lifetime > 0)
            {
                DiscardAfter(m_Lifetime);
            }
        }

        public void Discard()
        {
            DiscardAfter(0);
        }

        public void DiscardAfter(float secs)
        {
            if (m_Discard != null)
            {
                StopCoroutine(m_Discard);
            }

            if (secs > 0)
            {
                m_Discard = DiscardCR(secs);
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

        private IEnumerator DiscardCR(float secs = 0)
        {
            yield return new WaitForSeconds(secs);
            OnDiscard();
        }
    }
}