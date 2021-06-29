using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using MBaske.Sensors.Util;
using NaughtyAttributes;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Add this component to a gameobject for making it visible to the 
    /// <see cref="GridSensorComponent2D"/> or <see cref="GridSensorComponent3D"/>.
    /// A <see cref="DetectableGameObject"/> must have at least one enabled collider,
    /// which can be attached to a nested gameobject.
    /// </summary>
    public class DetectableGameObject : MonoBehaviour, IDetectable, IDisposable
    {
        /// <inheritdoc/>
        public string Tag => tag;

        /// <inheritdoc/>
        public string Name => name;

        private PhysicsScene m_PhysicsScene;
        private IList<Collider> m_Colliders;
        private bool m_IsCompound;
        private bool m_IsCached;

        [SerializeField, Tooltip("Optional settings for shape detection.")]
        private GameObjectShape m_Shape = new GameObjectShape();

        // Manage pending scans for ALL DetectableGameObject instances.
        public static bool HasPendingScans => s_NumberOfPendingScans > 0;

        private const int c_MaxScansPerFrame = 5;
        private static int s_NumberOfPendingScans;
        private static int s_ScanStartFrameCount;
        private static float s_ScanStartTime;
        private static bool s_PendingScansQueued;


        /// <summary>
        /// Sets detection state. Invoked after the <see cref="GameObjectDetector"/>
        /// first encountered this object, wasDetected = true, via shared cache methods. 
        /// Called again when cache is cleared, wasDetected = false.
        /// </summary>
        /// <param name="wasDetected">Detection state</param>
        public virtual void SetDetectionState(bool wasDetected)
        {
            if (wasDetected)
            {
                InitObservables();
                FindColliders();
                AddToCache();
            }
            else if (m_Colliders != null)
            {
                if (m_IsCached)
                {
                    RemoveFromCache();
                }
                m_Colliders.Clear();
            }
        }

        /// <summary>
        /// Forces a new runtime shape scan.
        /// </summary>
        public void ScanShapeRuntime(int waitFrames = 0)
        {
            s_NumberOfPendingScans++;

            if (s_NumberOfPendingScans > c_MaxScansPerFrame)
            {
                waitFrames += (s_NumberOfPendingScans - 1) / c_MaxScansPerFrame;

                if (!s_PendingScansQueued)
                {
                    Debug.LogWarning("Waiting for pending shape scans...");
                    s_PendingScansQueued = true;
                    s_ScanStartFrameCount = Time.frameCount;
                    s_ScanStartTime = Time.time;
                }
            }

            new InvokeAfterFrames(this, ScanShape, waitFrames);
        }

        // Button for manual shape scan in editor mode.
        // Press after changing colliders. Updating the
        // shape settings will trigger rescan automatically.
        [Button(enabledMode: EButtonEnableMode.Editor)]
        private void ScanShape()
        {
            FindColliders();

            int maxLOD = ShapeScanUtil.Scan(
                m_PhysicsScene, transform, m_Colliders,
                m_Shape, out List<ScanResultLOD> result);
            m_Shape.OnScanResult(maxLOD, result);

            s_NumberOfPendingScans = Mathf.Max(0, s_NumberOfPendingScans - 1);

            if (s_PendingScansQueued && s_NumberOfPendingScans == 0)
            {
                int df = Time.frameCount - s_ScanStartFrameCount;
                float dt = Time.time - s_ScanStartTime;
                Debug.Log($"All shape scans completed after {df} frames / {dt} seconds.");
                s_PendingScansQueued = false;
            }
        }

        private void FindColliders()
        {
            m_Colliders = new List<Collider>(GetComponentsInChildren<Collider>());

            for (int i = m_Colliders.Count - 1; i >= 0; i--)
            {
                if (!m_Colliders[i].enabled)
                {
                    m_Colliders.RemoveAt(i);
                }
            }
            
            Debug.Assert(m_Colliders.Count > 0, "No enabled colliders found on DetectableGameObject");
            m_IsCompound = m_Colliders.Count > 1;
        }

        private void ValidateColliderTags()
        {
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var cld in colliders)
            {
                if (!cld.CompareTag(tag))
                {
                    Debug.LogWarning($"A nested collider's tag must match its parent " +
                        $"DetectableGameObject's tag. Changing '{cld.tag}' to '{tag}' " +
                        $"for collider '{cld.name}'.");
                    cld.tag = tag;
                }
            }
        }


        #region Custom Observables

        /// <inheritdoc/>
        public ObservableCollection Observables { get; private set; }

        /// <inheritdoc/>
        public virtual ObservableCollection InitObservables()
        {
            Observables = new ObservableCollection();
            AddObservables();
            return Observables;
        }

        /// <inheritdoc/>
        public virtual void AddObservables() { }

        #endregion


        #region Detection

        /// <summary>
        /// Returns the gameobject's position to the <see cref="GameObjectDetector"/>.
        /// </summary>
        /// <returns>Gameobject's position in world space</returns>
        public Vector3 GetWorldPosition() => transform.position;

        /// <summary>
        /// Returns the closet point on the gameobject's collider(s) 
        /// to the <see cref="GameObjectDetector"/>.
        /// </summary>
        /// <param name="sensorPos">Sensor position in world space</param>
        /// <param name="onBounds">Whether to get the closest point on the 
        /// collider's bounds or on the collider itself</param>
        /// <returns>Closest point in world space</returns>
        public Vector3 GetClosestWorldPoint(Vector3 sensorPos, bool onBounds = true)
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

        /// <summary>
        /// Returns the <see cref="GameObjectShape"/> points to the <see cref="GameObjectDetector"/>.
        /// </summary>
        /// <param name="normDistance">Normalized distance between gameobject and sensor</param>
        /// <returns>List of points in world space</returns>
        public IList<Vector3> GetShapeWorldPoints(float normDistance)
        {
            return m_Shape.GetWorldPointsAtDistance(transform, normDistance);
        }

        #endregion


        #region MonoBehaviour Callbacks

        private void OnDrawGizmosSelected()
        {
            m_Shape.DrawGizmos(transform);
        }

        private void Awake()
        {
            HandleAwake();
        }

        protected virtual void HandleAwake()
        {
            ValidateColliderTags();
        }

        private void OnValidate()
        {
            HandleValidate();
        }

        protected virtual void HandleValidate()
        {
            // https://forum.unity.com/threads/raycast-in-the-prefab-scene.647548/#post-4339375
            var scene = gameObject.scene;
            m_PhysicsScene = scene.IsValid() &&
                PhysicsSceneExtensions.GetPhysicsScene(scene).IsValid()
                    ? PhysicsSceneExtensions.GetPhysicsScene(scene)
                    : Physics.defaultPhysicsScene;

            m_Shape.RequireScanEvent -= ScanShape;
            m_Shape.RequireScanEvent += ScanShape;
        }

        private void Reset()
        {
            HandleReset();
        }

        protected virtual void HandleReset()
        {
            SetDetectionState(false);
            m_Shape.Reset();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// Cleans up internal objects.
        /// </summary>
        public void Dispose()
        {
            SetDetectionState(false);
            Observables = null;
            m_Colliders = null;

            m_Shape.RequireScanEvent -= ScanShape;
        }

        #endregion


        #region Caching

        // Caches DetectableGameObjects after they were first encountered by 
        // the GameObjectDetector. Since we're using colliders as keys, the same 
        // object might be added multiple times if it has a compound collider.
        private static readonly IDictionary<Collider, DetectableGameObject> s_SharedCache
            = new Dictionary<Collider, DetectableGameObject>(); // capacity?

        /// <summary>
        /// Returns a <see cref="DetectableGameObject"/> instance from the cache.
        /// </summary>
        /// <param name="collider">The detected collider</param>
        /// <returns><see cref="DetectableGameObject"/> associated with collider</returns>
        public static DetectableGameObject GetCached(Collider collider)
        {
            if (s_SharedCache.TryGetValue(collider, out DetectableGameObject obj))
            {
                return obj;
            }

            // Collider not cached yet -> get its DetectableGameObject.

            obj = collider.GetComponentInParent<DetectableGameObject>();

            if (obj != null)
            {
                if (s_SharedCache.Values.Contains(obj))
                {
                    // Object was already added for a different collider (compound).
                    s_SharedCache.Add(collider, obj);
                }
                else
                {
                    // New DetectableGameObject, will invoke AddToCache.
                    obj.SetDetectionState(true);
                }

                return obj;
            }

            throw new KeyNotFoundException("No DetectableGameObject associated with collider " + collider);
        }

        /// <summary>
        /// Clears the cache and resets all stored <see cref="DetectableGameObject"/>s' detection states.
        /// </summary>
        public static void ClearCache()
        {
            var allObjects = new List<DetectableGameObject>(s_SharedCache.Values);

            foreach (var obj in allObjects)
            {
                // Will invoke RemoveFromCache.
                obj.SetDetectionState(false);
            }

            if (s_SharedCache.Count > 0)
            {
                Debug.LogWarning("Orphaned DetectableGameObjects found in cache " +
                    string.Join(", ", s_SharedCache.Values.Distinct().Select(o => o.name).ToArray()));

                s_SharedCache.Clear();
            }
        }

        private void AddToCache()
        {
            foreach (var collider in m_Colliders)
            {
                if (!s_SharedCache.ContainsKey(collider))
                {
                    s_SharedCache.Add(collider, this);
                }
                else
                {
                    Debug.LogWarning("Collider already added to cache " + collider);
                }
            }

            m_IsCached = true;
        }

        private void RemoveFromCache()
        {
            foreach (var collider in m_Colliders)
            {
                if (s_SharedCache.ContainsKey(collider))
                {
                    s_SharedCache.Remove(collider);
                }
                else
                {
                    Debug.LogWarning("Collider not found in cache " + collider);
                }
            }

            m_IsCached = false;
        }

        #endregion
    }

    /// <summary>
    /// Invokes callback after all pending scans are complete.
    /// </summary>
    public class InvokeOnShapeScansComplete : CustomYieldInstruction
    {
        /// <summary>
        /// Invokes callback after all pending scans are complete.
        /// </summary>
        /// <param name="context">The MonoBehaviour</param>
        /// <param name="callback">The callback method</param>
        public InvokeOnShapeScansComplete(MonoBehaviour context, Action callback)
        {
            context.StartCoroutine(Coroutine(callback));
        }

        public override bool keepWaiting => DetectableGameObject.HasPendingScans;

        private IEnumerator Coroutine(Action callback)
        {
            yield return this;
            callback.Invoke();
        }
    }
}
