using UnityEngine;

namespace MBaske.Driver
{
    /// <summary>
    /// <see cref="Poolable"/> obstacle: pole, cone, barrel or road block.
    /// </summary>
    public class Obstacle : Poolable
    {
        private Rigidbody m_Rigidbody;

        /// <inheritdoc/>
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