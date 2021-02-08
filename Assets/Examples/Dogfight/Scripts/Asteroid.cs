using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace MBaske.Dogfight
{
    public class Asteroid : MonoBehaviour
    {
        public Vector3 LocalPosition
        {
            get { return transform.localPosition; }
            set { transform.localPosition = value; }
        }

        public Vector3 WorldVelocity
        {
            get { return m_Rigidbody.velocity; }
            set { m_Rigidbody.velocity = value; }
        }

        public Vector3 WorldSpin
        {
            set { m_Rigidbody.angularVelocity = value; }
        }

        [Range(1f, 10f)]
        public float MinSize = 4;
        [Range(1f, 10f)]
        public float MaxSize = 8;
        [Range(0f, 0.9f)]
        public float MaxDeform = 0.75f;
        [Range(0f, 2f)]
        public float Noise = 1;

        [SerializeField]
        private Mesh m_IcoMesh;
        private Mesh m_Mesh;

        private Rigidbody m_Rigidbody;
        private MeshFilter m_MeshFilter;
        private MeshCollider m_MeshCollider;

        private void OnValidate()
        {
            MinSize = Mathf.Min(MinSize, MaxSize);
        }

        private void Awake()
        {
            m_Mesh = Instantiate(m_IcoMesh);
            m_Rigidbody = GetComponent<Rigidbody>();
            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshCollider = GetComponent<MeshCollider>();
        }

        public void RandomizeShape(float minSize, float maxSize, float maxDeform, float noise)
        {
            MinSize = minSize;
            MaxSize = maxSize;
            MaxDeform = maxDeform;
            Noise = noise;
            RandomizeShape();
        }

        public void RandomizeShape()
        {
            // Default mesh radius is 1, size 2x2x2.
            Vector3 radius = Vector3.one + Random.insideUnitSphere * MaxDeform;
            radius *= Random.Range(MinSize, MaxSize) * 0.5f;
            transform.localScale = radius;
            m_Rigidbody.mass = radius.x * radius.y * radius.z * 10;

            float a = Random.Range(0.5f, 2f);
            float b = Random.Range(0.25f, 0.5f) * (Random.value > 0.5f ? Noise : -Noise);

            Vector3[] verts = m_Mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = m_IcoMesh.vertices[i] * (1 + noise.cnoise(verts[i] * a) * b);
            }

            m_Mesh.vertices = verts;
            m_Mesh.RecalculateNormals();
            m_Mesh.RecalculateBounds();

            m_MeshFilter.sharedMesh = m_Mesh;
            m_MeshCollider.sharedMesh = m_Mesh;
        }
    }
}