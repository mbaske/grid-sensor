using UnityEngine;

namespace MBaske.Dogfight
{
    public interface IBulletOwner
    {
        Vector3 GunPosition { get; }
        Vector3 GunDirection { get; }
        void OnBulletHitScored();
    }

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