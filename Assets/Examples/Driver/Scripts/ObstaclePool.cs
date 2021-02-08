using UnityEngine;

namespace MBaske.Driver
{
    public class ObstaclePool : Pool<Obstacle>
    {
        public Obstacle Spawn(int index, Vector3 pos, Quaternion rot)
        {
            var obj = Spawn(pos, index);
            obj.transform.rotation = rot;
            return (Obstacle)obj;
        }
    }
}