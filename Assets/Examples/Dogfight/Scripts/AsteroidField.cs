using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MBaske.Dogfight
{
    public class AsteroidField : MonoBehaviour
    {
        public float FieldRadius = 100;
        private float m_RadiusSqr;
        [SerializeField, Range(10, 50)]
        private float m_AsteroidSpacing = 22;
        [SerializeField, ReadOnly]
        private int m_AsteroidCount; // info
        private int m_ShipCount;
        private int m_UpdateIndex;

        [Header("Asteroid Motion")]
        [SerializeField, Range(0f, 10f)]
        private float m_MaxVelocity = 5;
        [SerializeField, Range(0f, 5f)]
        private float m_MaxSpin = 2;
        [Header("Asteroid Shape")]
        [SerializeField, Range(1f, 10f)]
        private float m_MinSize = 4;
        [SerializeField, Range(1f, 10f)]
        private float m_MaxSize = 8;
        [SerializeField, Range(0f, 0.9f)]
        private float m_MaxDeform = 0.75f;
        [SerializeField, Range(0f, 2f)]
        private float m_Noise = 1;
        [Space, SerializeField]
        private Asteroid m_AsteroidPrefab;

        private List<Asteroid> m_Asteroids;
        private List<Vector3> m_AsteroidPositions;
        private List<Spaceship> m_Ships;
        private List<Vector3> m_ShipPositions;


#if (UNITY_EDITOR)
        private void OnValidate()
        {
            ClampProperties();
        }
#endif

        private void Awake()
        {
            ClampProperties();
            Initialize();
            Randomize();
        }

        // Called by each agent.
        // Randomize when all agents are ready.
        public void OnEpisodeBegin()
        {
            if (++m_ShipCount == m_Ships.Count)
            {
                m_ShipCount = 0;
                Randomize();
            }
        }

        private void Update()
        {
            // Bounce back if positions are out of bounds.
            // Staggered check, one asteroid per update step.

            var asteroid = m_Asteroids[m_UpdateIndex];
            m_UpdateIndex = ++m_UpdateIndex % m_Asteroids.Count;

            if (asteroid.LocalPosition.sqrMagnitude > m_RadiusSqr)
            {
                if (Vector3.Dot(asteroid.LocalPosition, asteroid.WorldVelocity) > 0)
                {
                    asteroid.WorldVelocity *= -1;
                }
            }
        }

        private void ClampProperties()
        {
            m_MaxSize = Mathf.Min(m_MaxSize, m_AsteroidSpacing * 0.5f);
            m_MinSize = Mathf.Min(m_MinSize, m_MaxSize);
            FieldRadius = Mathf.Max(FieldRadius, m_AsteroidSpacing);
            m_RadiusSqr = FieldRadius * FieldRadius;
        }

        private void Initialize()
        {
            m_Asteroids = new List<Asteroid>();
            m_AsteroidPositions = new List<Vector3>();

            m_Ships = new List<Spaceship>(FindObjectsOfType<Spaceship>());
            m_ShipPositions = new List<Vector3>();

            foreach (Vector3 p in AsteroidPositions())
            {
                var asteroid = Instantiate(m_AsteroidPrefab, transform);
                asteroid.RandomizeShape(m_MinSize, m_MaxSize, m_MaxDeform, m_Noise);
                asteroid.LocalPosition = p;
                m_Asteroids.Add(asteroid);
                m_AsteroidPositions.Add(p);

                if (HasAssociatedShipPosition(p, out Vector3 q))
                {
                    m_ShipPositions.Add(q);
                }
            }
        }

        private void Randomize()
        {
            m_AsteroidPositions = Shuffle(m_AsteroidPositions);
            for (int i = 0; i < m_Asteroids.Count; i++)
            {
                m_Asteroids[i].LocalPosition = m_AsteroidPositions[i];
                m_Asteroids[i].WorldVelocity = Random.insideUnitSphere * m_MaxVelocity;
                m_Asteroids[i].WorldSpin = Random.insideUnitSphere * m_MaxSpin;
            }

            m_ShipPositions = Shuffle(m_ShipPositions);
            for (int i = 0; i < m_Ships.Count; i++)
            {
                m_Ships[i].LocalPosition = m_ShipPositions[i];
            }

            m_UpdateIndex = 0;
        }

        private IEnumerable<Vector3> AsteroidPositions()
        {
            m_AsteroidCount = 0;
            int ext = Mathf.CeilToInt(FieldRadius / m_AsteroidSpacing);

            for (int x = -ext; x <= ext; x++)
            {
                for (int y = -ext; y <= ext; y++)
                {
                    for (int z = -ext; z <= ext; z++)
                    {
                        Vector3 p = new Vector3(x, y, z) * m_AsteroidSpacing;

                        if (p.sqrMagnitude <= m_RadiusSqr)
                        {
                            m_AsteroidCount++;
                            yield return p;
                        }
                    }
                }
            }
        }

        private bool HasAssociatedShipPosition(Vector3 asteroidPos, out Vector3 shipPos)
        {
            shipPos = asteroidPos + Vector3.one * m_AsteroidSpacing * 0.5f;
            return shipPos.sqrMagnitude <= m_RadiusSqr * 0.5f;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 pos = transform.position;
            float radius = m_MaxSize * 0.5f;

            foreach (var p in AsteroidPositions())
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(pos + p, radius);

                if (HasAssociatedShipPosition(p, out Vector3 q))
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(pos + q, 1);
                }
            }
        }

        private static List<Vector3> Shuffle(List<Vector3> list)
        {
            var rnd = new System.Random();
            return list.OrderBy(x => rnd.Next()).ToList();
        }
    }
}