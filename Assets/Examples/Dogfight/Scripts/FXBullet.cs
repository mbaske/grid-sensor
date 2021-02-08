using UnityEngine;

namespace MBaske.Dogfight
{
    public class FXBullet : Bullet
    {
        private TrailRenderer m_Trail;

        protected override void Initialize()
        {
            base.Initialize();

            m_Trail = GetComponent<TrailRenderer>();
            m_Trail.enabled = false;
        }

        public override void OnSpawn()
        {
            base.OnSpawn();

            m_Trail.Clear();
            m_Trail.enabled = true;
        }

        protected override void OnDiscard()
        {
            base.OnDiscard();

            m_Trail.enabled = false;
        }

        protected override void OnCollision(Collider other)
        {
            m_Trail.enabled = false;
        }
    }
}