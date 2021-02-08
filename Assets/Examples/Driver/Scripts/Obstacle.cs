using UnityEngine;

namespace MBaske.Driver
{
    public class Obstacle : Poolable
    {
        private Rigidbody m_Rigidbody;

        public override void OnSpawn()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            base.OnSpawn();
        }

        protected override void OnDiscard()
        {
            if (m_Rigidbody != null)
            {
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
                m_Rigidbody.Sleep();
            }
            base.OnDiscard();
        }
    }
}