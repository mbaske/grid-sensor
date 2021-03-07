using System;
using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Static utility class for scanning one or more colliders.
    /// </summary>
    public static class ColliderScanner
    {
        private static Collider[] s_Buffer = new Collider[8];
        private static IDictionary<Vector3Int, IList<Vector3>>[] s_Merged;
        private static bool[,,] s_Filled;
        private static bool[,,] s_Sparse;
        private static int m_Size;

        private static void Allocate(int size)
        {
            m_Size = size;
            s_Filled = new bool[size, size, size];
            s_Sparse = new bool[size, size, size];

            int n = Mathf.CeilToInt(Mathf.Log(size, 2)) - 1;
            s_Merged = new Dictionary<Vector3Int, IList<Vector3>>[n];
            for (int i = 0; i < n; i++)
            {
                s_Merged[i] = new Dictionary<Vector3Int, IList<Vector3>>(); // capacity?
            }
        }

        private static void Clear(int size)
        {
            if (size > m_Size)
            {
                Allocate(size);
            }
            else
            {
                Array.Clear(s_Filled, 0, s_Filled.Length);
                Array.Clear(s_Sparse, 0, s_Sparse.Length);

                for (int i = 0; i < s_Merged.Length; i++)
                {
                    s_Merged[i].Clear();
                }
            }
        }


        public static List<Vector3>[] Scan(
            PhysicsScene physicsScene, Transform transform,
            IList<Collider> colliders, float resolution, bool is2D)
        {
            List<Vector3>[] result;

            int mask = 1 << transform.gameObject.layer;
            var matrix = transform.worldToLocalMatrix;

            int nColliders = colliders.Count;
            if (nColliders > s_Buffer.Length)
            {
                s_Buffer = new Collider[nColliders];
            }

            for (int i = 0; i < nColliders; i++)
            {
                if (colliders[i] is MeshCollider && !((MeshCollider)colliders[i]).convex)
                {
                    Debug.LogWarning($"Setting mesh collider '{transform.name}' to convex.");
                    ((MeshCollider)colliders[i]).convex = true;
                }
            }

            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < nColliders; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
            
            // Round bounds to resolution.
            Vector3Int units = new Vector3Int(
                Mathf.RoundToInt(bounds.size.x / resolution),
                Mathf.RoundToInt(bounds.size.y / resolution),
                Mathf.RoundToInt(bounds.size.z / resolution));
            units = Vector3Int.Max(units, Vector3Int.one);
            bounds.size = (Vector3)units * resolution;
            // Offset for centering points in grid cells.
            Vector3 offset = bounds.min + Vector3.one * resolution * 0.5f;


            // Early return for single point.

            if (units == Vector3Int.one)
            {
                result = new List<Vector3>[] { new List<Vector3>(1) };
                result[0].Add(matrix.MultiplyPoint3x4(bounds.center));
                
                return result;
            }


            // Early return for 2D.

            if (is2D)
            {
                var flat = new Dictionary<Vector2Int, Vector3>(units.x * units.z);

                for (int x = 0; x < units.x; x++)
                {
                    for (int y = 0; y < units.y; y++)
                    {
                        for (int z = 0; z < units.z; z++)
                        {
                            Vector3 p = offset + new Vector3(x, y, z) * resolution;
                            int n = physicsScene.OverlapSphere(p, 0, s_Buffer, mask,
                                QueryTriggerInteraction.UseGlobal);
                            if (n > 0)
                            {
                                for (int i = 0; i < nColliders; i++)
                                {
                                    // Check buffer against colliders, so we don't
                                    // include points inside overlapping colliders
                                    // of neighbouring objects.
                                    if (Array.IndexOf(s_Buffer, colliders[i]) != -1)
                                    {
                                        p.y = bounds.center.y;
                                        flat[new Vector2Int(x, z)] = p;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                
                result = new List<Vector3>[] { new List<Vector3>(flat.Values) };
                for (int i = 0, n = result[0].Count; i < n; i++)
                {
                    result[0][i] = matrix.MultiplyPoint3x4(result[0][i]);
                }
                
                return result;
            }


            // 3D

            int unitMax = Mathf.Max(units.x, units.y, units.z);
            Clear(unitMax);
            
            // Analyze collider bounds.
            for (int x = 0; x < units.x; x++)
            {
                for (int y = 0; y < units.y; y++)
                {
                    for (int z = 0; z < units.z; z++)
                    {
                        Vector3 p = offset + new Vector3(x, y, z) * resolution;
                        int n = physicsScene.OverlapSphere(p, 0, s_Buffer, mask, 
                            QueryTriggerInteraction.UseGlobal);
                        if (n > 0)
                        {
                            for (int i = 0; i < nColliders; i++)
                            {
                                // Check buffer against colliders, so we don't
                                // include points inside overlapping colliders
                                // of neighbouring objects.
                                if (Array.IndexOf(s_Buffer, colliders[i]) != -1)
                                {
                                    s_Filled[x, y, z] = true;
                                    s_Sparse[x, y, z] = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Reduce point count, hollow out.
            units -= Vector3Int.one;
            for (int x = 1; x < units.x; x++)
            {
                for (int y = 1; y < units.y; y++)
                {
                    for (int z = 1; z < units.z; z++)
                    {
                        s_Sparse[x, y, z] &=
                            !(s_Filled[x - 1, y, z]
                            & s_Filled[x + 1, y, z]
                            & s_Filled[x, y - 1, z]
                            & s_Filled[x, y + 1, z]
                            & s_Filled[x, y, z - 1]
                            & s_Filled[x, y, z + 1]
                        );
                    }
                }
            }

            Vector3 unitCenter = new Vector3(units.x * 0.5f, units.y * 0.5f, units.z * 0.5f);
            units += Vector3Int.one;

            // Detail levels for result.
            int nLevel = Mathf.CeilToInt(Mathf.Log(unitMax, 2)) + 1;
            // Merge points of higher detail levels, see keys below.
            int nMerge = nLevel - 2;

            result = new List<Vector3>[nLevel];
            for (int i = 0; i < nLevel; i++)
            {
                result[i] = new List<Vector3>(); // capacity?
            }

            for (int x = 0; x < units.x; x++)
            {
                for (int y = 0; y < units.y; y++)
                {
                    for (int z = 0; z < units.z; z++)
                    {
                        if (s_Sparse[x, y, z])
                        {
                            Vector3 u = new Vector3(x, y, z);
                            Vector3 p = matrix.MultiplyPoint3x4(offset + u * resolution); 
                            result[0].Add(p); // all points, highest detail.

                            u -= unitCenter; // shift units, e.g. 0 to +8 -> -4 to +4

                            for (int i = 0; i < nMerge; i++)
                            {
                                u *= 0.5f; // half detail.

                                // Keys are tree-like, e.g.
                                // merged[0] 8/6/6 -> centroid -> result[1]
                                // merged[1] 4/3/3 -> centroid -> result[2]
                                // merged[2] 2/1/1 -> centroid -> result[3]
                                // ...
                                // List for each key contains points of prior
                                // level keys/lists, to be averaged for less detail.
                                Vector3Int k = new Vector3Int(
                                    Mathf.FloorToInt(u.x),
                                    Mathf.FloorToInt(u.y),
                                    Mathf.FloorToInt(u.z));

                                if (!s_Merged[i].TryGetValue(k, out IList<Vector3> list))
                                {
                                    list = new List<Vector3>(); // capacity?
                                    s_Merged[i].Add(k, list);
                                }

                                list.Add(p);
                            }
                        }
                    }
                }
            }

            // Calculate centroids.
            for (int i = 0; i < nMerge; i++)
            {
                int j = i + 1;
                foreach (var list in s_Merged[i].Values)
                {
                    Vector3 sum = Vector3.zero;
                    int n = list.Count;
                    for (int k = 0; k < n; k++)
                    {
                        sum += list[k];
                    }
                    result[j].Add(sum / n);
                }
            }

            // Single center point for last level.
            result[nLevel - 1].Add(matrix.MultiplyPoint3x4(bounds.center));

            return result;
        }
    }
}
