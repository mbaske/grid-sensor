using UnityEngine;

namespace MBaske.Dogfight
{
    /// <summary>
    /// Interface for bullet owner who must provide the position and
    /// direction of the gun, as well as a callback for reacting to scored hits.
    /// </summary>
    public interface IBulletOwner
    {
        /// <summary>
        /// Where to place the gun.
        /// </summary>
        Vector3 GunPosition { get; }

        /// <summary>
        /// Where to point the gun.
        /// </summary>
        Vector3 GunDirection { get; }

        /// <summary>
        /// Callback performs actions after a bullet hit was scored.
        /// </summary>
        void OnBulletHitScored();
    }

    /// <summary>
    /// <see cref="Poolable"/> bullet.
    /// </summary>
    public class Bullet : Poolable
    {
        [SerializeField]
        protected float m_Force = 100;
        [SerializeField]
        protected string m_TargetTag = "Spaceship";

        protected Rigidbody m_Rigidbody;
        protected IBulletOwner m_Owner;

        private void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Shoots a <see cref="Bullet"/> associated 
        /// with a specified <see cref="IBulletOwner"/>.
        /// </summary>
        /// <param name="owner"><see cref="IBulletOwner"/></param>
        public void Shoot(IBulletOwner owner)
        {
            m_Owner = owner;
            m_Rigidbody.velocity = owner.GunDirection * m_Force;
        }

        protected virtual void OnCollision(Collider other)
        {
            if (other.CompareTag(m_TargetTag))
            {
                m_Owner.OnBulletHitScored();
            }
            DiscardAfter(0);
        }

        protected override void OnDiscard()
        {
            base.OnDiscard();

            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
            m_Rigidbody.Sleep();
            m_Owner = null;
        }

        // Use trigger collider if the bullet 
        // shouldn't knock the spaceship off course.
        private void OnTriggerEnter(Collider other)
        {
            OnCollision(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            OnCollision(collision.collider);
        }
    }
}