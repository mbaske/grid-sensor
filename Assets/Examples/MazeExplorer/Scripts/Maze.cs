using System.Collections.Generic;
using MBaske.Sensors.Grid;
using UnityEngine;

namespace MBaske.MazeExplorer
{
    /// <summary>
    /// Generates a random maze and its mesh, plus food items.
    /// The maze is NOT a true maze in the sense that it could 
    /// be 'solved' by finding a particular path.
    /// </summary>
    public class Maze : MonoBehaviour
    {
        public const int NumChannels = 3;
        // Buffer channels.
        public const int Wall = 0;
        public const int Food = 1;
        public const int Visit = 2;

        public GridBuffer Buffer;

        [SerializeField]
        [Tooltip("Whether to create any meshes. They aren't required by the agent.")]
        private bool m_GenerateMeshes = true;

        [SerializeField]
        private float m_FoodProbablility = 0.04f;

        [SerializeField]
        private GameObject m_FoodPrefab;

        private Stack<GameObject> m_FoodPool;
        private Dictionary<Vector2Int, GameObject> m_FoodByPos;

        private Dictionary<Vector3Int, Vector4> m_Face;
        private List<Vector3> m_Vertices;
        private List<Vector3> m_Normals;
        private List<int> m_Triangles;
        private Mesh m_Mesh;

        /// <summary>
        /// Randomizes maze on episode begin.
        /// </summary>
        /// <returns>Agent spawn position</returns>
        public Vector2Int Randomize()
        {
            if (m_GenerateMeshes)
            {
                ClearFood();
            }

            Buffer.Clear();

            int w = Buffer.Width - 1;
            int h = Buffer.Height - 1;
            Vector2Int spawnPos = default;

            // Walls.

            for (int x = 0; x <= w; x++)
            {
                for (int y = 0; y <= h; y++)
                {
                    if (x == 0 || y == 0 || x == w || y == h)
                    {
                        Buffer.Write(Wall, x, y, 1);
                    }
                    else if (x % 2 == 0 && y % 2 == 0)
                    {
                        Buffer.Write(Wall, x, y, 1);

                        int a = Util.RandomBool()
                            ? 0
                            : (Util.RandomBool() ? -1 : 1);
                        int b = a != 0
                            ? 0
                            : (Util.RandomBool() ? -1 : 1);
                        
                        Buffer.Write(Wall, x + a, y + b, 1);
                    }
                }
            }

            // Spawn and food positions.

            for (int x = 1; x < w; x++)
            {
                for (int y = 1; y < h; y++)
                {
                    if (Buffer.Read(Wall, x, y) == 0)
                    {
                        if (spawnPos == default)
                        {
                            spawnPos = new Vector2Int(x, y);
                        }
                        else if (Util.RandomBool(m_FoodProbablility))
                        {
                            AddFood(new Vector2Int(x, y));
                        }
                    }
                }
            }


            if (m_GenerateMeshes)
            {
                UpdateMesh();
            }

            return spawnPos;
        }


        // Food.

        private void ClearFood()
        {
            if (m_FoodPool == null)
            {
                m_FoodPool = new Stack<GameObject>();
                m_FoodByPos = new Dictionary<Vector2Int, GameObject>();
            }
            else
            {
                var keys = new List<Vector2Int>(m_FoodByPos.Keys);
                foreach (Vector2Int pos in keys)
                {
                    RemoveFood(pos);
                }

                m_FoodByPos.Clear();
            }
        }

        private void AddFood(Vector2Int pos)
        {
            if (m_GenerateMeshes)
            {
                var food = m_FoodPool.Count > 0
                    ? m_FoodPool.Pop()
                    : Instantiate(m_FoodPrefab, transform);
                food.transform.position = new Vector3(pos.x, 0, pos.y);
                food.SetActive(true);
                m_FoodByPos.Add(pos, food);
            }

            Buffer.Write(Food, pos, 1);
        }

        /// <summary>
        /// Removes food item after it was found by <see cref="MazeAgent"/>.
        /// </summary>
        /// <param name="pos">Food's grid position</param>
        public void RemoveFood(Vector2Int pos)
        {
            if (m_GenerateMeshes)
            {
                var food = m_FoodByPos[pos];
                food.SetActive(false);
                m_FoodPool.Push(food);
                m_FoodByPos.Remove(pos);
            }
            
            Buffer.Write(Food, pos, 0);
        }


        // Mesh.

        private void InitMesh()
        {
            m_Mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            m_Mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = m_Mesh;

            m_Face = new Dictionary<Vector3Int, Vector4>();
            m_Vertices = new List<Vector3>();
            m_Normals = new List<Vector3>();
            m_Triangles = new List<int>();
        }

        private void UpdateMesh()
        {
            if (m_Mesh == null)
            {
                InitMesh();
            }

            m_Mesh.Clear();
            m_Vertices.Clear();
            m_Normals.Clear();
            m_Triangles.Clear();

            int w = Buffer.Width;
            int h = Buffer.Height;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Vector3Int pos = new Vector3Int(x, 0, y);
                    if (HasWallAt(pos))
                    {
                        AddFace(pos, Vector3Int.up);
                        AddFace(pos, Vector3Int.down);
                        TryAddFace(pos, Vector3Int.left);
                        TryAddFace(pos, Vector3Int.right);
                        TryAddFace(pos, Vector3Int.forward);
                        TryAddFace(pos, Vector3Int.back);
                    }
                }
            }

            m_Mesh.SetVertices(m_Vertices);
            m_Mesh.SetNormals(m_Normals);
            m_Mesh.SetTriangles(m_Triangles, 0);
        }

        private void TryAddFace(Vector3Int pos, Vector3Int offset)
        {
            if (!HasWallAt(pos + offset))
            {
                AddFace(pos, offset);
            }
        }

        private void AddFace(Vector3Int pos, Vector3Int offset)
        {
            m_Face.Clear();

            // Center of wall surface between pos and pos + offset in doubled coordiantes.
            // We double the coordinates in oder to get Vector2Int keys for wall vertices.
            Vector3Int p = pos * 2 + offset;
           
            if (offset == Vector3Int.left)
            {
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.up + Vector3Int.back));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.down + Vector3Int.back));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.down + Vector3Int.forward));

                m_Triangles.Add(AddVertex(offset, p + Vector3Int.down + Vector3Int.forward));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.up + Vector3Int.forward));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.up + Vector3Int.back));
            }
            else if (offset == Vector3Int.right)
            {
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.down + Vector3Int.forward));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.down + Vector3Int.back));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.up + Vector3Int.back));

                m_Triangles.Add(AddVertex(offset, p + Vector3Int.up + Vector3Int.back));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.up + Vector3Int.forward));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.down + Vector3Int.forward));
            }
            if (offset == Vector3Int.down)
            {
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.left + Vector3Int.back));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.right + Vector3Int.back));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.right + Vector3Int.forward));

                m_Triangles.Add(AddVertex(offset, p + Vector3Int.right + Vector3Int.forward));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.left + Vector3Int.forward));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.left + Vector3Int.back));
            }
            if (offset == Vector3Int.up)
            {
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.left + Vector3Int.back));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.left + Vector3Int.forward));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.right + Vector3Int.forward));
                
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.right + Vector3Int.forward));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.right + Vector3Int.back));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.left + Vector3Int.back));
            }
            if (offset == Vector3Int.back)
            {
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.left + Vector3Int.down));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.left + Vector3Int.up));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.right + Vector3Int.up));

                m_Triangles.Add(AddVertex(offset, p + Vector3Int.right + Vector3Int.up));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.right + Vector3Int.down));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.left + Vector3Int.down));
            }
            if (offset == Vector3Int.forward)
            {
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.right + Vector3Int.down));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.right + Vector3Int.up));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.left + Vector3Int.up));

                m_Triangles.Add(AddVertex(offset, p + Vector3Int.left + Vector3Int.up));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.left + Vector3Int.down));
                m_Triangles.Add(AddVertex(offset, p + Vector3Int.right + Vector3Int.down));
            }
        }

        private int AddVertex(Vector3Int offset, Vector3Int doubledVertexPos)
        {
            if (!m_Face.TryGetValue(doubledVertexPos, out Vector4 vertex))
            {
                vertex = (Vector3)doubledVertexPos * 0.5f;
                vertex.w = m_Vertices.Count; // vertex index
                m_Face.Add(doubledVertexPos, vertex);
                m_Vertices.Add(vertex);
                m_Normals.Add(offset);
            }

            return (int)vertex.w;
        }

        private bool HasWallAt(Vector3Int pos)
        {
            return Buffer.TryRead(Wall, pos.x, pos.z, out float value) && value == 1;
        }
    }
}