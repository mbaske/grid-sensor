using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Serializable wrapper for scan results.
    /// </summary>
    [Serializable]
    public class ScanResultLOD
    {
        /// <summary>
        /// LOD points in <see cref="DetectableGameObject"/>'s space.
        /// </summary>
        public List<Vector3> LocalPoints = new List<Vector3>();

        /// <summary>
        /// <see cref="ScanResultLOD"/> list factory.
        /// </summary>
        /// <param name="LODCount">Number of LODs</param>
        /// <returns>List of <see cref="ScanResultLOD"/> instances</returns>
        public static List<ScanResultLOD> CreateList(int LODCount)
        {
            var list = new List<ScanResultLOD>();
            for (int i = 0; i < LODCount; i++)
            {
                list.Add(new ScanResultLOD());
            }

            return list;
        }
    }

    /// <summary>
    /// Static utility class for scanning a <see cref="GameObjectShape"/>.
    /// </summary>
    public static class ShapeScanUtil
    {
        /// <summary>
        /// A <see cref="Volume"/> contains one or more connected colliders.
        /// <see cref="Volume"/>s are split into <see cref="Cell"/>s sitting in a 3D grid.
        /// (Not to be confused with the observed grid.)
        /// </summary>
        private class Volume
        {
            /// <summary>
            /// Highest available LOD in <see cref="Volume"/>.
            /// </summary>
            public int MaxLOD;
            /// <summary>
            /// Colliders in <see cref="Volume"/>.
            /// </summary>
            public List<Collider> Colliders;
            /// <summary>
            /// Enclosing world bounds for all colliders in <see cref="Volume"/>.
            /// </summary>
            public Bounds Bounds;
            /// <summary>
            /// <see cref="Cell"/> size in world units.
            /// </summary>
            public Vector3 CellSize;
            /// <summary>
            /// The number of <see cref="Cell"/>s on each axis.
            /// </summary>
            public Vector3Int GridSize;
            /// <summary>
            /// Whether the <see cref="Volume"/> contains concave colliders.
            /// </summary>
            public bool IsConcave;
        }

        private static readonly List<Volume> s_Volumes = new List<Volume>();

        // Highest LOD in any volume.
        private static int s_GlobalMaxLOD;

        /// <summary>
        /// A <see cref="Cell"/> is associated with a <see cref="PointGroup"/>.
        /// </summary>
        private struct Cell
        {
            /// <summary>
            /// <see cref="Cell"/> level equals LOD for 3D, 
            /// or is 0  when object shape is flattened for 2D.
            /// </summary>
            public int level;

            /// <summary>
            /// <see cref="Cell"/> x grid position.
            /// </summary>
            public int x;
            /// <summary>
            /// <see cref="Cell"/> y grid position.
            /// </summary>
            public int y;
            /// <summary>
            /// <see cref="Cell"/> z grid position.
            /// </summary>
            public int z;

            /// <summary>
            /// Creates a <see cref="Cell"/> instance.
            /// </summary>
            /// <param name="level">Cell level</param>
            /// <param name="x"><see cref="Cell"/> x grid position</param>
            /// <param name="y"><see cref="Cell"/> y grid position</param>
            /// <param name="z"><see cref="Cell"/> z grid position</param>
            public Cell(int level, int x, int y, int z)
            {
                this.level = level;
                this.x = x;
                this.y = y;
                this.z = z;
            }

            /// <summary>
            /// Reduces the <see cref="Cell"/>'s level and grid position.
            /// Eight neighbouring <see cref="Cell"/> on level n are 
            /// sampled down to one <see cref="Cell"/> on level n - 1.
            /// </summary>
            /// <returns></returns>
            public Cell Downsample()
            {
                return new Cell(level - 1, x / 2, y / 2, z / 2);
            }

            /// <summary>
            /// Multiplies a Vector3 with the <see cref="Cell"/>'s grid position.
            /// </summary>
            /// <param name="v">The Vector3 to scale</param>
            /// <returns>Scaled vector</returns>
            public Vector3 Scale(Vector3 v)
            {
                return new Vector3(x * v.x, y * v.y, z * v.z);
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return string.Format("Cell {0}-{1},{2},{3}", level, x, y, z);
            }
        }

        /// <summary>
        /// <see cref="PointGroup"/> calculates the centroid for all points 
        /// associated with a <see cref="Cell"/>.
        /// </summary>
        private class PointGroup
        {
            private int m_Count;
            private Vector3 m_Sum;

            /// <summary>
            /// Centroid of added points.
            /// </summary>
            public Vector3 Centroid => m_Sum / m_Count;

            /// <summary>
            /// Adds point.
            /// </summary>
            /// <param name="point">Point to add</param>
            public void AddPoint(Vector3 point)
            {
                m_Count++;
                m_Sum += point;
            }
        }

        private static readonly Dictionary<Cell, PointGroup> s_PointGroupsByCell
            = new Dictionary<Cell, PointGroup>();

 
        // Buffer for Physics.Overlap checks.
        private static Collider[] s_PhysicsBuffer;

        private static void CheckPhysicsBufferSize(IList<Collider> colliders)
        {
            if (s_PhysicsBuffer == null || colliders.Count > s_PhysicsBuffer.Length)
            {
                s_PhysicsBuffer = new Collider[colliders.Count];
            }
        }


        // 3D grids for buffering cell states
        // -> true: inside collider, false: outside.
        private static bool[,,] s_FilledGrid;
        private static bool[,,] s_HollowGrid;
        private static Vector3Int m_GridSize;

        private static void Clear(Vector3Int size)
        {
            bool isLarger = 
                size.x > m_GridSize.x || 
                size.y > m_GridSize.y || 
                size.z > m_GridSize.z;
            bool isMuchSmaller = size.magnitude < m_GridSize.magnitude * 0.5f;

            if (isLarger || isMuchSmaller)
            {
                s_FilledGrid = new bool[size.x, size.y, size.z];
                s_HollowGrid = new bool[size.x, size.y, size.z];
                m_GridSize = size;
            }
            else
            {
                Array.Clear(s_FilledGrid, 0, s_FilledGrid.Length);
                Array.Clear(s_HollowGrid, 0, s_HollowGrid.Length);
            }

            s_PointGroupsByCell.Clear();
        }


        /// <summary>
        /// Performs a scan over a set of colliders and returns
        /// lists of enclosed points for different levels of detail.
        /// </summary>
        /// <param name="scene"><see cref="PhysicsScene"/> for raycasts and overlap checks</param>
        /// <param name="transform"><see cref="DetectableGameObject"/> transform</param>
        /// <param name="colliders">List of colliders</param>
        /// <param name="shape"><see cref="GameObjectShape"/></param>
        /// <param name="result">List of <see cref="ScanResultLOD"/> instances (output)</param>
        /// <returns>The highest LOD for the given scan settings and object size</returns>
        public static int Scan(
            PhysicsScene scene, 
            Transform transform,
            IList<Collider> colliders, 
            GameObjectShape shape,
            out List<ScanResultLOD> result)
        {
            s_GlobalMaxLOD = 0;

            CheckPhysicsBufferSize(colliders);
            CreateVolumes(colliders, shape);

            result = shape.ScanLOD > 0
                ? GetLODPointLists(scene, transform, shape)
                : GetCenterPointList(transform, shape);

            // GlobalMaxLOD corresponds to the result list's
            // length unless the 'flatten' option is enabled:
            // GlobalMaxLOD = result.Length - 1
            //
            // If the points are flattened for 2D, the result
            // only contains a single point list, because dynamic 
            // LODs are unnecessary for 2D detection. 
            // However, GlobalMaxLOD still refers to the number
            // of LODs that *would* be used given the current 
            // Scan LOD without flattening. The reason for this
            // is that we need the max LOD value for showing
            // the correct inspector settings, even if the scan 
            // result contains only one point list.
            //
            // GlobalMaxLOD is also used for clamping the
            // Scan LOD setting, because we have a minimum  
            // length limit when splitting volumes into cells.
            // Therefore it's not possible to apply high Scan 
            // LODs to small colliders/volumes.

            return s_GlobalMaxLOD;
        }

        private static void CreateVolumes(
            IList<Collider> colliders, 
            GameObjectShape shape)
        {
            s_Volumes.Clear();

            if (shape.Merge)
            {
                // Merge all.

                var volume = new Volume()
                {
                    Colliders = new List<Collider>(colliders)
                };

                foreach (var collider in colliders)
                {
                    bool isConcave = collider is MeshCollider
                        && !((MeshCollider)collider).convex;

                    volume.IsConcave = volume.IsConcave || isConcave;
                    volume.Bounds.Encapsulate(collider.bounds);
                }
                s_Volumes.Add(volume);
            }
            else
            {
                var byColliderType = new List<Volume>[]
                {
                    new List<Volume>(), new List<Volume>()
                };

                foreach (var collider in colliders)
                {
                    bool isConcave = collider is MeshCollider
                        && !((MeshCollider)collider).convex;

                    byColliderType[isConcave ? 1 : 0].Add(new Volume()
                    {
                        Colliders = new List<Collider>() { collider },
                        Bounds = collider.bounds,
                        IsConcave = isConcave
                    });
                }

                // Merge connected by type, don't mix convex and concave.

                for (int i = 0; i < 2; i++)
                {
                    var volumes = byColliderType[i];

                    for (int j = volumes.Count - 1; j > 0; j--)
                    {
                        for (int k = j - 1; k > -1; k--)
                        {
                            // NOTE Intersects check yields true for colliders
                            // located side by side, e.g. two box colliders of
                            // size 1x1x1, sitting at 0/0/0 and 1/1/1 respectively.
                            if (volumes[j].Bounds.Intersects(volumes[k].Bounds))
                            {
                                volumes[j].Bounds.Encapsulate(volumes[k].Bounds);
                                volumes[j].Colliders.AddRange(volumes[k].Colliders);
                                volumes.RemoveAt(k);
                                j--;
                            }
                        }
                    }

                    s_Volumes.AddRange(volumes);
                }
            }
        }

        private static List<ScanResultLOD> GetCenterPointList(
            Transform transform, 
            GameObjectShape shape)
        {
            // Single ScanResultLOD for lowest LOD.
            var result = ScanResultLOD.CreateList(1);

            Matrix4x4 matrix = transform.worldToLocalMatrix;
            bool flatten = shape.Flatten;

            foreach (Volume vol in s_Volumes)
            {
                Vector3 localPoint = matrix.MultiplyPoint3x4(vol.Bounds.center);
                if (flatten)
                {
                    localPoint.y = 0;
                }
                result[0].LocalPoints.Add(localPoint);
            }

            return result;
        }

        private static List<ScanResultLOD> GetLODPointLists(
            PhysicsScene scene, 
            Transform transform, 
            GameObjectShape shape)
        {
            SplitVolumesIntoCells(shape.ScanLOD);

            int mask = 1 << transform.gameObject.layer;
            bool flatten = shape.Flatten;

            var result = ScanResultLOD.CreateList(flatten ? 1 : s_GlobalMaxLOD + 1);

            foreach (Volume vol in s_Volumes)
            {
                ToggleActiveColliders(vol);

                bool isConcave = vol.IsConcave;
                Vector3 cellSize = vol.CellSize;
                Vector3Int gridSize = vol.GridSize;
                Vector3 offset = vol.Bounds.min + cellSize * 0.5f;
                Vector3 boundsCenter = vol.Bounds.center;
                float boundsMagnitude = vol.Bounds.size.magnitude;
                int nx = gridSize.x - 1;
                int ny = gridSize.y - 1;
                int nz = gridSize.z - 1;


                // Find cells occupied by colliders.

                Clear(gridSize);

                for (int x = 0; x <= nx; x++)
                {
                    for (int y = 0; y <= ny; y++)
                    {
                        for (int z = 0; z <= nz; z++)
                        {
                            Vector3 worldPoint = Vector3.Scale(
                                new Vector3(x, y, z), cellSize) + offset;

                            bool isInside = isConcave
                                ? IsInsideCollider(
                                    scene, mask, worldPoint, vol.Bounds)
                                : IsInsideCollider(
                                    scene, mask, worldPoint, vol.Colliders);

                            s_HollowGrid[x, y, z] = isInside;
                            s_FilledGrid[x, y, z] = isInside;
                        }
                    }
                }


                // Reduce cell count, hollow out filled grid.

                for (int x = 1; x < nx; x++)
                {
                    for (int y = 1; y < ny; y++)
                    {
                        for (int z = 1; z < nz; z++)
                        {
                            if (s_FilledGrid[x, y, z])
                            {
                                s_HollowGrid[x, y, z] &=
                                !(s_FilledGrid[x - 1, y, z]
                                & s_FilledGrid[x + 1, y, z]
                                & s_FilledGrid[x, y - 1, z]
                                & s_FilledGrid[x, y + 1, z]
                                & s_FilledGrid[x, y, z - 1]
                                & s_FilledGrid[x, y, z + 1]);
                            }
                        }
                    }
                }


                // Downsample cells for generating LODs 
                // and store points in corresponding groups.

                int level = flatten ? 0 : vol.MaxLOD;
                float yFlat = transform.position.y;

                for (int x = 0; x <= nx; x++)
                {
                    for (int y = 0; y <= ny; y++)
                    {
                        for (int z = 0; z <= nz; z++)
                        {
                            if (s_HollowGrid[x, y, z])
                            {
                                Cell cell = new Cell(level, x, y, z);
                                Vector3 worldPoint = cell.Scale(cellSize) + offset;

                                if (flatten)
                                {
                                    cell.y = 0;
                                    worldPoint.y = yFlat;
                                }

                                // Highest LOD.
                                AddPoint(cell, worldPoint);

                                for (int i = level; i > 0; i--)
                                {
                                    cell = cell.Downsample();
                                    AddPoint(cell, worldPoint);
                                }
                            }  
                        }
                    }
                }


                // Write centroids to result.

                Matrix4x4 matrix = transform.worldToLocalMatrix;
                float projection = shape.Projection;
                bool project = projection > 0;

                foreach (var kvp in s_PointGroupsByCell)
                {
                    // KeyValuePair, Key: Cell, Value: PointGroup.
                    int i = kvp.Key.level;
                    Vector3 worldPoint = kvp.Value.Centroid;

                    if (project && i > 0)
                    {
                        // Project outward from volume bounds center, 
                        // unless lowest LOD or flattened points -> i == 0.
                        // NOTE Doesn't work well with compound or concave colliders.
                        Vector3 normal = (worldPoint - boundsCenter).normalized;
                        if (scene.Raycast(boundsCenter + normal * boundsMagnitude,
                            -normal, out RaycastHit hit, boundsMagnitude, mask))
                        {
                            worldPoint = Vector3.Lerp(worldPoint, hit.point, projection);
                        }
                    }
                    // World -> local.
                    result[i].LocalPoints.Add(matrix.MultiplyPoint3x4(worldPoint));
                }
            }

            ToggleActiveColliders();

            return result;
        }

        private static void SplitVolumesIntoCells(
            int resolution, 
            float globalMinLength = 0.1f)
        {
            // We're splitting up volumes individually, but consider
            // the global max volume length for each. This way we get
            // somewhat consistent cell sizes for disconnected volumes.
            //
            // NOTE Picking a global LOD that matches volumes of very  
            // different sizes is difficult. Better keep sizes similar.

            float globalMaxLength = 0;
            foreach (Volume vol in s_Volumes)
            {
                Vector3 s = vol.Bounds.size;
                globalMaxLength = Mathf.Max(globalMaxLength, s.x, s.y, s.z);
            }

            // Split each volume into a grid of size volume.GridSize (indices)
            // with each of its cells having the size volume.CellSize (world units).

            foreach (Volume vol in s_Volumes)
            {
                float length = globalMaxLength;
                int[] gridSize = new int[] { 1, 1, 1 };
                Vector3 s = vol.Bounds.size;
                float[] cellSize = new float[] { s.x, s.y, s.z };
                // Lower length limit. Cells can be smaller than this
                // value, but won't be split any further if they are.
                float min = Mathf.Max(
                    globalMinLength, length / Mathf.Pow(2, resolution));

                do
                {
                    length *= 0.5f;
                    // Set threshold at x 1.5 for creating squarish cells.
                    // splitThresh = length x 1 would stretch cells too much
                    // before splitting occurs.
                    float splitThresh = length * 1.5f;

                    // Split along each axis individually.
                    for (int i = 0; i < 3; i++)
                    {
                        if (cellSize[i] > splitThresh)
                        {
                            cellSize[i] *= 0.5f;
                            gridSize[i] *= 2;
                        }
                    }
                }
                while (length > min);

                vol.CellSize = new Vector3(cellSize[0], cellSize[1], cellSize[2]);
                vol.GridSize = new Vector3Int(gridSize[0], gridSize[1], gridSize[2]);
                vol.MaxLOD = Mathf.RoundToInt(Mathf.Log(gridSize.Max(), 2));
                s_GlobalMaxLOD = Mathf.Max(s_GlobalMaxLOD, vol.MaxLOD);
            }
        }

        private static void AddPoint(Cell cell, Vector3 point)
        {
            if (!s_PointGroupsByCell.TryGetValue(cell, out PointGroup group))
            {
                group = new PointGroup();
                s_PointGroupsByCell.Add(cell, group);
            }
            group.AddPoint(point);
        }

        // Convex.
        private static bool IsInsideCollider(
            PhysicsScene scene, 
            int mask, 
            Vector3 point,
            IList<Collider> colliders)
        {
            // NOTE PhysicsScene doesn't support OverlapSphereNonAlloc.
            if (scene.OverlapSphere(
                point, 
                0, 
                s_PhysicsBuffer, 
                mask, 
                QueryTriggerInteraction.UseGlobal) > 0)
            {
                foreach (var collider in colliders)
                {
                    // Check buffer against colliders, so we don't
                    // include points inside overlapping colliders
                    // of neighbouring objects.
                    if (Array.IndexOf(s_PhysicsBuffer, collider) != -1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Concave.
        private static bool IsInsideCollider(
            PhysicsScene scene, 
            int mask, 
            Vector3 point,
            Bounds bounds)
        {
            bounds.Expand(0.1f); // Pad.
            Vector4[] sides = new Vector4[]
            {
                // Ray origins, w = ray length.
                new Vector4(bounds.min.x, point.y, point.z, point.x - bounds.min.x),
                new Vector4(bounds.max.x, point.y, point.z, bounds.max.x - point.x),
                new Vector4(point.x, bounds.min.y, point.z, point.y - bounds.min.y),
                new Vector4(point.x, bounds.max.y, point.z, bounds.max.y - point.y),
                new Vector4(point.x, point.y, bounds.min.z, point.z - bounds.min.z),
                new Vector4(point.x, point.y, bounds.max.z, bounds.max.z - point.z)
            };

            for (int i = 0; i < 6; i++)
            {
                // NOTE PhysicsScene doesn't support linecast.
                // TODO Check against volume colliders?
                if (!scene.Raycast(
                    sides[i], 
                    point - (Vector3)sides[i], 
                    sides[i].w, 
                    mask))
                {
                    return false;
                }
            }
            // TODO Can we ignore all cells the rays have 
            // already passed through at later cell checks?
            return true;
        }

        private static void ToggleActiveColliders(Volume volume = null)
        {
            foreach (Volume vol in s_Volumes)
            {
                foreach (var collider in vol.Colliders)
                {
                    collider.enabled = volume == null || volume == vol;
                }
            }
        }
    }
}
