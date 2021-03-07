using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace MBaske.Sensors.Grid
{
    public abstract partial class DetectableGameObject : MonoBehaviour, IDetectable
    {
        private static readonly IDictionary<Collider, DetectableGameObject> s_SharedCache
            = new Dictionary<Collider, DetectableGameObject>(); // capacity?

        private static void AddToCache(Collider cld, DetectableGameObject obj)
        {
            if (!s_SharedCache.ContainsKey(cld))
            {
                s_SharedCache.Add(cld, obj);
            }
            else
            {
                Debug.LogWarning("Collider already added to cache " + cld);
            }
        }

        private static void RemoveFromCache(Collider cld)
        {
            if (s_SharedCache.ContainsKey(cld))
            {
                s_SharedCache.Remove(cld);
            }
            else
            {
                Debug.LogWarning("Collider not found in cache " + cld);
            }
        }

        public static DetectableGameObject GetCached(Collider cld)
        {
            if (s_SharedCache.TryGetValue(cld, out DetectableGameObject obj))
            {
                return obj;
            }

            // Not cached yet -> get component.

            obj = cld.GetComponentInParent<DetectableGameObject>();

            if (obj != null)
            {
                // Initialize on-demand, will invoke AddToCache
                // and add all of the gameobject's colliders.
                obj.RuntimeInitialize();
                return obj;
            }

            throw new KeyNotFoundException("No DetectableGameObject associated with collider " + cld);
        }

        public static void ClearCache()
        {
            var list = new List<DetectableGameObject>(s_SharedCache.Values);
            for (int i = 0, n = list.Count; i < n; i++)
            {
                // Will invoke RemoveFromCache.
                list[i].Deinitialize();
            }

            if (s_SharedCache.Count > 0)
            {
                Debug.LogWarning("Orphaned DetectableGameObjects found in cache " +
                    string.Join(", ", s_SharedCache.Values.Distinct().Select(o => o.name).ToArray()));
                s_SharedCache.Clear();
            }
        }
    }
}