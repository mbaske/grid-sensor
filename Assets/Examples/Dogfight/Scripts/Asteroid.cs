using UnityEngine;
using Unity.Mathematics;
using Random = UnityEngine.Random;
using NaughtyAttributes;

namespace MBaske.Dogfight
{
    /// <summary>
    /// Randomizable asteroid.
    /// </summary>
    public class Asteroid : MonoBehaviour
    {
        /// <summary>
        /// Asteroid's position in local space.
        /// </summary>
        public Vector3 LocalPosition
        {
            get { return transform.localPosition; }
            set { transform.localPosition = value; }
        }

        /// <summary>
        /// Asteroid's velocity in world space.
        /// </summary>
        public Vector3 WorldVelocity
        {
            get { return m_Rigidbody.velocity; }
            set { m_Rigidbody.velocity = value; }
        }

        /// <summary>
        /// Asteroid's angular velocity in world space.
        /// </summary>
        public Vector3 WorldSpin
        {
            set { m_Rigidbody.angularVelocity = value; }
        }

        [Space]
        [Range(1f, 100f)]
        public float MinSize = 4;
        [Range(1f, 100f)]
        public float MaxSize = 8;
        [Range(0f, 0.9f)]
        public float MaxDeform = 0.75f;
        [Range(0f, 2f)]
        public float Noise = 1;

        [SerializeField]
        private Mesh m_Template;
        private Rigidbody m_Rigidbody;

        /// <summary>
        /// Randomizes the asteroid's shape.
        /// </summary>
        /// <param name="minSize">Minimum size</param>
        /// <param name="maxSize">Maximum size</param>
        /// <param name="maxDeform">Maximum deform</param>
        /// <param name="noise">Noise amount</param>
        public void RandomizeShape(float minSize, float maxSize, float maxDeform, float noise)
        {
            MinSize = minSize;
            MaxSize = maxSize;
            MaxDeform = maxDeform;
            Noise = noise;
            UpdateShape(true);
        }

        private void OnValidate()
        {
            MinSize = Mathf.Min(MinSize, MaxSize);
        }

        [Button]
        private void DebugRandomize()
        {
            UpdateShape(true);
        }

        [Button]
        private void DebugReset()
        {
            UpdateShape(false);
        }

        /// <summary>
        /// Updates the asteroids shape and mass.
        /// </summary>
        /// <param name="randomize">Whether to randomize the 
        /// asteroid or reset it to default values</param>
        public void UpdateShape(bool randomize)
        {
            float a = Random.Range(0.5f, 2f);
            float b = Random.Range(0.25f, 0.5f) * (Random.value > 0.5f ? Noise : -Noise);

            Mesh ??= Instantiate(m_Template);
            Vector3[] vtx = m_Template.vertices;
            for (int i = 0; i < vtx.Length; i++)
            {
                vtx[i] = m_Template.vertices[i] * (randomize ? (1 + noise.cnoise(vtx[i] * a) * b) : 1);
            }
            Mesh.vertices = vtx;
            Mesh.RecalculateNormals();
            Mesh.RecalculateBounds();
            GetComponent<MeshCollider>().sharedMesh = Mesh;

            // Default mesh radius is 1, size 2x2x2.
            Vector3 radius = Vector3.one + Random.insideUnitSphere * MaxDeform;
            radius *= Random.Range(MinSize, MaxSize) * 0.5f;
            transform.localScale = randomize ? radius : Vector3.one;

            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.mass = randomize ? radius.x * radius.y * radius.z * 10 : 1;
        }

        private Mesh Mesh
        {
            get { return GetComponent<MeshFilter>().sharedMesh; }
            set { GetComponent<MeshFilter>().sharedMesh = value; }
        }

        private void OnDestroy()
        {
            Destroy(Mesh);
        }
    }
}