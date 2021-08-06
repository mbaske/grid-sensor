using UnityEngine;
using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Detects gameobjects located in 3D space.
    /// </summary>
    public class GameObjectDetector3D : GameObjectDetector
    {
        /// <summary>
        /// Constraint for spherical / 3D detection.
        /// </summary>
        public DetectionConstraint3D Constraint
        {
            set { m_Constraint = value; ValidateCullingEnabled(); }
        }
        private DetectionConstraint3D m_Constraint;

        // Optional frustum culling for convex FOV.
        private HashSet<Collider> m_ColliderBufferSet;
        private bool m_CullingEnabled;
        private Camera m_Camera;
        private Plane[] m_Planes;

        private void ValidateCullingEnabled()
        {
            m_CullingEnabled = GeomUtil3D.HasValidFrustum(m_Constraint.LonLatRect,
                Mathf.Max(m_Constraint.MinRadius, 0.01f), m_Constraint.MaxRadius,
                out Frustum frustum);

            if (m_CullingEnabled)
            {
                // Create a helper camera for valid frustum, 
                // so we can cull the found colliders before the
                // detector has to parse all their shape points.

                var nested = m_Transform.Find("HelperCam");
                if (nested == null)
                {
                    nested = new GameObject("HelperCam").transform;
                    // Cam frustum does update even with disabled
                    // camera component and inactive gameobject.
                    nested.gameObject.SetActive(false);
                    nested.parent = m_Transform;
                    nested.localPosition = Vector3.zero;

                    m_Camera = nested.gameObject.AddComponent<Camera>();
                    m_Camera.transform.localRotation = Quaternion.identity;
                    m_Camera.cullingMask = m_LayerMask;
                    m_Camera.enabled = false;
                }
                else
                {
                    m_Camera = nested.GetComponent<Camera>();
                }

                m_Camera.nearClipPlane = frustum.Near;
                m_Camera.farClipPlane = frustum.Far;
                m_Camera.projectionMatrix = GeomUtil3D.GetProjectionMatrix(frustum);

                m_ColliderBufferSet = new HashSet<Collider>();
                m_Planes = new Plane[6];
            }
        }

        /// <inheritdoc/>
        public override void OnSensorUpdate()
        {
            base.OnSensorUpdate();

            int numFound = 0;
            do
            {
                ValidateColliderBufferSize(numFound);
                numFound = Physics.OverlapSphereNonAlloc(
                    m_Transform.position,
                    m_Constraint.MaxRadius, 
                    m_ColliderBuffer, 
                    m_LayerMask);
            }
            while (numFound == m_ColliderBufferSize);


            if (m_CullingEnabled)
            {
                m_ColliderBufferSet.Clear();
                GeometryUtility.CalculateFrustumPlanes(m_Camera, m_Planes);

                for (int i = 0; i < numFound; i++)
                {
                    var cld = m_ColliderBuffer[i];
                    if (GeometryUtility.TestPlanesAABB(m_Planes, cld.bounds))
                    {
                        m_ColliderBufferSet.Add(cld);
                    }
                }
                m_ColliderBufferSet.CopyTo(m_ColliderBuffer);
                //Debug.Log($"Culled: {numFound} > {m_ColliderBufferSet.Count}");
                numFound = m_ColliderBufferSet.Count;
            }

            ParseColliders(numFound, m_Constraint, m_Transform.worldToLocalMatrix);
        }
    }
}