using UnityEngine;

namespace MBaske.Dogfight
{
    /// <summary>
    /// Concrete pool for <see cref="Bullet"/>s.
    /// </summary>
    public class BulletPool : Pool<Bullet>
    {
        /// <summary>
        /// Shoots a <see cref="Bullet"/> associated 
        /// with a specified <see cref="IBulletOwner"/>.
        /// </summary>
        /// <param name="owner"><see cref="IBulletOwner"/></param>
        public void Shoot(IBulletOwner owner)
        {
            Spawn(owner.GunPosition).Shoot(owner);
        }
    }
}