using UnityEngine;

namespace MBaske.Dogfight
{
    public class BulletPool : Pool<Bullet>
    {
        public void Shoot(IBulletOwner owner)
        {
            ((Bullet)Spawn(owner.GunPosition)).Shoot(owner);
        }
    }
}