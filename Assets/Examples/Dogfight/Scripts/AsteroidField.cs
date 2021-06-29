using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using NaughtyAttributes;
using MBaske.Sensors.Grid;
using Random = UnityEngine.Random;

namespace MBaske.Dogfight
{
    /// <summary>
    /// Generates a bunch of random <see cref="Asteroid"/>s and 
    /// finds spawn positions for the <see cref="Spaceship"/>s.
    /// </summary>
    public class AsteroidField : MonoBehaviour
    {
        /// <summary>
        /// Optional: Invoked after all shape scans are done.
        /// </summary>
        public event Action ScansCompleteEvent;
        private bool m_OptScansComplete; // flag

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
            RandomizePositions();
        }

        /// <summary>
        /// Invoked by <see cref="PilotAgent"/>.
        /// Randomizes asteroid field when all agents are ready.
        /// </summary>
        public void OnEpisodeBegin()
        {
            if (++m_ShipCount == m_Ships.Count)
            {
                m_ShipCount = 0;
                RandomizePositions();
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
            m_Asteroids = new List<Asteroid>(m_AsteroidCount);
            m_AsteroidPositions = new List<Vector3>(m_AsteroidCount);

            m_Ships = new List<Spaceship>(FindObjectsOfType<Spaceship>());
            m_ShipPositions = new List<Vector3>(m_AsteroidCount / 2);

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

        private void RandomizePositions()
        {
            m_AsteroidPositions = Shuffle(m_AsteroidPositions);
            for (int i = 0; i < m_Asteroids.Count; i++)
            {
                m_Asteroids[i].LocalPosition = m_AsteroidPositions[i];
                m_Asteroids[i].WorldVelocity = Vector3.zero;
                m_Asteroids[i].WorldSpin = Vector3.zero;
            }

            m_ShipPositions = Shuffle(m_ShipPositions);
            for (int i = 0, n = Mathf.Min(m_Ships.Count, m_ShipPositions.Count); i < n; i++)
            {
                m_Ships[i].LocalPosition = m_ShipPositions[i];
            }

            m_UpdateIndex = 0;


            // Optional: Scan asteroids AFTER they're spread out,
            // but BEFORE moving them around.

            if (!m_OptScansComplete)
            {
                foreach (var asteroid in m_Asteroids)
                {
                    if (asteroid.TryGetComponent(out DetectableGameObject obj))
                    {
                        obj.ScanShapeRuntime();
                    }
                    else
                    {
                        m_OptScansComplete = true;
                        break;
                    }
                }
            }

            if (m_OptScansComplete)
            {
                RandomizeVelocities();
            }
            else
            {
                new InvokeOnShapeScansComplete(this, OnShapeScansComplete);
            }
        }

        private void OnShapeScansComplete()
        {
            m_OptScansComplete = true;
            ScansCompleteEvent?.Invoke();
            RandomizeVelocities();
        }

        private void RandomizeVelocities()
        {
            foreach (var asteroid in m_Asteroids)
            {
                asteroid.WorldVelocity = Random.insideUnitSphere * m_MaxVelocity;
                asteroid.WorldSpin = Random.insideUnitSphere * m_MaxSpin;
            }
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