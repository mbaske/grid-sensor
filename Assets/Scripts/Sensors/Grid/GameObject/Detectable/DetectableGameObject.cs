using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Base class for detectable gameobjects. 
    /// </summary>
    public abstract partial class DetectableGameObject : MonoBehaviour, IDetectable
    {
        public string Tag => tag; // IDetectable

        protected PhysicsScene m_PhysicsScene;
        protected IList<Collider> m_Colliders;
        protected bool m_HasColliders;
        protected bool m_IsCompound;
        protected bool m_IsCached;

        protected abstract GameObjectShape Shape { get; }
        protected abstract void ResetShape();

        private void Reset()
        {
            ResetShape();
            Deinitialize();
            FindColliders();
        }

        private void OnDisable()
        {
            Deinitialize();
        }

        private void OnValidate()
        {
            // https://forum.unity.com/threads/raycast-in-the-prefab-scene.647548/#post-4339375
            var s = gameObject.scene;
            if (s.IsValid() && PhysicsSceneExtensions.GetPhysicsScene(s).IsValid())
            {
                m_PhysicsScene = PhysicsSceneExtensions.GetPhysicsScene(s);
            }

            Shape.OnValidate();
        }


        // Called on-demand by shared cache when 
        // detector first encounters this gameobject.
        public void RuntimeInitialize()
        {
            if (Shape.ScanAtRuntime)
            {
                Shape.IsScanned = false;
            }

            FindColliders();
            AddToCache();

            InitObservations();
        }

        public void Deinitialize()
        {
            if (m_IsCached)
            {
                RemoveFromCache();
            }
            m_HasColliders = false;
        }

        public void ForceRescan()
        {
            Shape.IsScanned = false;
            if (Application.isPlaying)
            {
                StartCoroutine(DeinitializeCR());
            }
        }

        private IEnumerator DeinitializeCR()
        {
            yield return new WaitForEndOfFrame();
            Deinitialize();
        }



        // CUSTOM OBSERVATIONS

        public Observations Observations { get; private set; }

        public virtual Observations InitObservations()
        {
            Observations = new Observations();
            AddObservations();
            return Observations;
        }

        public virtual void AddObservations() { }

        protected virtual float OneHot() => 1;



        // CALLED BY DETECTOR

        public Vector3 GetPosition() => transform.position;

        public Vector3 GetClosestPoint(Vector3 sensorPos, bool onBounds = true)
        {
            Vector3 closest = onBounds
                ? m_Colliders[0].ClosestPointOnBounds(sensorPos)
                : m_Colliders[0].ClosestPoint(sensorPos);

            if (m_IsCompound)
            {
                float min = (closest - sensorPos).sqrMagnitude;
                for (int i = 1, n = m_Colliders.Count; i < n; i++)
                {
                    Vector3 p = onBounds
                        ? m_Colliders[i].ClosestPointOnBounds(sensorPos)
                        : m_Colliders[i].ClosestPoint(sensorPos);

                    float d = (p - sensorPos).sqrMagnitude;
                    if (d < min)
                    {
                        min = d;
                        closest = p;
                    }
                }
            }

            return closest;
        }

        public IList<Vector3> GetShapePoints(float normDistance)
        {
            if (!Shape.IsScanned)
            {
                ScanColliders();
            }
            return Shape.LocalToWorld(transform, normDistance);
        }



        // COLLIDERS & CACHE

        protected void FindColliders()
        {
            m_Colliders = new List<Collider>(GetComponentsInChildren<Collider>());

            for (int i = m_Colliders.Count - 1; i >= 0; i--)
            {
                if (!m_Colliders[i].enabled)
                {
                    m_Colliders.RemoveAt(i);
                }
            }

            m_IsCompound = m_Colliders.Count > 1;
            m_HasColliders = m_Colliders.Count > 0;

            Debug.Assert(m_HasColliders, "No enabled colliders found on DetectableGameObject");
        }

        protected void ScanColliders()
        {
            if (m_HasColliders)
            {
                if (m_PhysicsScene == null)
                {
                    m_PhysicsScene = Physics.defaultPhysicsScene;
                }
                Shape.OnScanResult(ColliderScanner.Scan(
                    m_PhysicsScene, transform, m_Colliders,
                    Shape.ScanResolution,
                    Shape is GameObjectShape2D));
            }
        }

        protected void AddToCache()
        {
            for (int i = 0, n = m_Colliders.Count; i < n; i++)
            {
                AddToCache(m_Colliders[i], this);
            }
            m_IsCached = true;
        }

        protected void RemoveFromCache()
        {
            for (int i = 0, n = m_Colliders.Count; i < n; i++)
            {
                RemoveFromCache(m_Colliders[i]);
            }
            m_IsCached = false;
        }


        // GIZMOS

        protected void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                FindColliders(); // always update

                if (m_HasColliders)
                {
                    if (Shape.GizmoForceScan())
                    {
                        ScanColliders();
                    }

                    if (Shape.GizmoDrawGrid(out float resolution))
                    {
                        Vector3 scale = Vector3.one * resolution;

                        Color colWire = Color.blue * 0.75f;
                        Color colFill = Color.blue * 0.4f;

                        var worldPoints = Shape.LocalToWorld(transform);
                        var filterDuplicates = new HashSet<Vector3>();

                        for (int i = 0, n = worldPoints.Count; i < n; i++)
                        {
                            var rounded = new Vector3(
                                Mathf.RoundToInt(worldPoints[i].x / resolution),
                                Mathf.RoundToInt(worldPoints[i].y / resolution),
                                Mathf.RoundToInt(worldPoints[i].z / resolution)) * resolution;

                            if (filterDuplicates.Add(rounded))
                            {
                                Gizmos.color = colWire;
                                Gizmos.DrawWireCube(rounded, scale);
                                Gizmos.color = colFill;
                                Gizmos.DrawCube(rounded, scale);
                            }
                        }
                    }
                }
            }

            if (Shape.GizmoHasPoints(out IList<Vector3> localPoints))
            {
                Color colWire = new Color(0, 0.5f, 0.5f, 0.4f);
                Color colFill = new Color(1, 0.5f, 0.5f, 1);

                Gizmos.matrix = transform.localToWorldMatrix;
                Vector3 scale = new Vector3(
                    1 / transform.lossyScale.x,
                    1 / transform.lossyScale.y,
                    1 / transform.lossyScale.z) * Shape.ScanResolution;

                for (int i = 0, n = localPoints.Count; i < n; i++)
                {
                    Gizmos.color = colWire;
                    Gizmos.DrawWireCube(localPoints[i], scale);
                    Gizmos.color = colFill;
                    Gizmos.DrawCube(localPoints[i], scale * 0.05f);
                }
            }
        }
    }
}
