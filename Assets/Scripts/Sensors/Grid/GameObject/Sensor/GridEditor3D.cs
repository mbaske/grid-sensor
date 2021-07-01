#if (UNITY_EDITOR)
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using MBaske.Sensors.Util;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Scene GUI editor for <see cref="GridSensorComponent3D"/>.
    /// Attached to <see cref="GridEditorHelper3D"/>.
    /// </summary>
    [CustomEditor(typeof(GridEditorHelper3D)), CanEditMultipleObjects]
    public class GridEditor3D : Editor
    {
        private GridSensorComponent3D m_Comp;
        private Quaternion[,] m_Wireframe;

        private ArcHandle m_ArcHandleLatN;
        private ArcHandle m_ArcHandleLatS;
        private ArcHandle m_ArcHandleLon;

        private static readonly Color s_WireColorA = new Color(0f, 0.5f, 1f, 0.3f);
        private static readonly Color s_WireColorB = new Color(0f, 0.5f, 1f, 0.1f);

        private void OnEnable()
        {
            m_ArcHandleLatN = new ArcHandle();
            m_ArcHandleLatN.SetColorWithoutRadiusHandle(new Color32(152, 237, 67, 255), 0.1f);
            m_ArcHandleLatN.radiusHandleColor = Color.white;
            
            m_ArcHandleLatS = new ArcHandle();
            m_ArcHandleLatS.SetColorWithoutRadiusHandle(new Color32(152, 237, 67, 255), 0.1f);
            m_ArcHandleLatS.radiusHandleColor = Color.white;
           
            m_ArcHandleLon = new ArcHandle();
            m_ArcHandleLon.SetColorWithoutRadiusHandle(new Color32(237, 67, 30, 255), 0.1f);
            m_ArcHandleLon.radiusHandleColor = Color.white;
        }

        private void OnSceneGUI()
        {
            m_Comp ??= ((GridEditorHelper3D)target).GetSensorComponent();

            if (m_Comp)
            {
                DrawHandles();
                DrawWireFrame();
            }
        }

        private void DrawHandles()
        {
            var tf = m_Comp.transform;

            // Latitude North & Max. Distance.

            Vector3 fwd = tf.forward;
            Vector3 normal = Vector3.Cross(fwd, tf.up);
            Matrix4x4 matrix = Matrix4x4.TRS(
                tf.position,
                Quaternion.LookRotation(fwd, normal),
                Vector3.one
            );

            using (new Handles.DrawingScope(matrix))
            {
                EditorGUI.BeginChangeCheck();
                {
                    m_ArcHandleLatN.angle = m_Comp.LatAngleNorth;
                    m_ArcHandleLatN.radius = m_Comp.MaxDistance;
                    m_ArcHandleLatN.DrawHandle();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    if (m_Comp.LatAngleNorth != m_ArcHandleLatN.angle)
                    {
                        m_Comp.LatAngleNorth = m_ArcHandleLatN.angle;
                    }
                    else if (m_Comp.MaxDistance != m_ArcHandleLatN.radius)
                    {
                        m_Comp.MaxDistance = m_ArcHandleLatN.radius;
                    }
                }
            }

            // Latitude South & Max. Distance.

            normal = Vector3.Cross(fwd, -tf.up);
            matrix = Matrix4x4.TRS(
                tf.position,
                Quaternion.LookRotation(fwd, normal),
                Vector3.one
            );

            using (new Handles.DrawingScope(matrix))
            {
                EditorGUI.BeginChangeCheck();
                {
                    m_ArcHandleLatS.angle = m_Comp.LatAngleSouth;
                    m_ArcHandleLatS.radius = m_Comp.MaxDistance;
                    m_ArcHandleLatS.DrawHandle();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    if (m_Comp.LatAngleSouth != m_ArcHandleLatS.angle)
                    {
                        m_Comp.LatAngleSouth = m_ArcHandleLatS.angle;
                    }
                    else if (m_Comp.MaxDistance != m_ArcHandleLatS.radius)
                    {
                        m_Comp.MaxDistance = m_ArcHandleLatS.radius;
                    }
                }
            }

            // Longitude & Max. Distance.

            normal = Vector3.Cross(fwd, tf.right);
            matrix = Matrix4x4.TRS(
                tf.position,
                Quaternion.LookRotation(fwd, normal),
                Vector3.one
            );

            using (new Handles.DrawingScope(matrix))
            {
                EditorGUI.BeginChangeCheck();
                {
                    m_ArcHandleLon.angle = m_Comp.LonAngle;
                    m_ArcHandleLon.radius = m_Comp.MaxDistance;
                    m_ArcHandleLon.DrawHandle();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    if (m_Comp.LonAngle != m_ArcHandleLon.angle)
                    {
                        m_Comp.LonAngle = m_ArcHandleLon.angle;
                    }
                    else if (m_Comp.MaxDistance != m_ArcHandleLon.radius)
                    {
                        m_Comp.MaxDistance = m_ArcHandleLon.radius;
                    }
                }
            }

            // Minimum Distance.

            float min = m_Comp.MinDistance;
            EditorGUI.BeginChangeCheck();
            {
                min = Handles.RadiusHandle(tf.rotation, tf.position, min);
            }
            if (EditorGUI.EndChangeCheck())
            {
                m_Comp.MinDistance = min;
            }
        }

        private void DrawWireFrame()
        {
            GeomUtil3D.CalcGridRotations(m_Comp.CellArc, 
                m_Comp.LonLatRect, m_Comp.GridShape, 
                ref m_Wireframe);
            
            EditorUtil.GLMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(m_Comp.transform.localToWorldMatrix);

            Vector3 min = Vector3.forward * m_Comp.MinDistance;
            Vector3 max = Vector3.forward * m_Comp.MaxDistance;

            // Grid Cells

            int nLon = m_Comp.GridShape.Width;
            int nLat = m_Comp.GridShape.Height;

            for (int iLat = 0; iLat <= nLat; iLat++)
            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(s_WireColorA);
                for (int iLon = 0; iLon <= nLon; iLon++)
                {
                    var v = m_Wireframe[iLon, iLat] * max;
                    GL.Vertex3(v.x, v.y, v.z);
                }
                GL.End();
            }
            
            for (int iLon = 0; iLon <= nLon; iLon++)
            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(s_WireColorA);
                for (int iLat = 0; iLat <= nLat; iLat++)
                {
                    var v = m_Wireframe[iLon, iLat] * max;
                    GL.Vertex3(v.x, v.y, v.z);
                }
                GL.End();
            }

            // Angles

            if (m_Comp.LatAngleSouth < 90)
            {
                GL.Begin(GL.LINES);
                GL.Color(s_WireColorB);
                for (int iLon = 0; iLon <= nLon; iLon++)
                {
                    var a = m_Wireframe[iLon, 0] * min;
                    GL.Vertex3(a.x, a.y, a.z);
                    var b = m_Wireframe[iLon, 0] * max;
                    GL.Vertex3(b.x, b.y, b.z);
                }
                GL.End();
            }

            if (m_Comp.LatAngleNorth < 90)
            {
                GL.Begin(GL.LINES);
                GL.Color(s_WireColorB);
                for (int iLon = 0; iLon <= nLon; iLon++)
                {
                    var a = m_Wireframe[iLon, nLat] * min;
                    GL.Vertex3(a.x, a.y, a.z);
                    var b = m_Wireframe[iLon, nLat] * max;
                    GL.Vertex3(b.x, b.y, b.z);
                }
                GL.End();
            }

            if (m_Comp.LonAngle < 180)
            {
                GL.Begin(GL.LINES);
                GL.Color(s_WireColorB);
                for (int iLat = 0; iLat <= nLat; iLat++)
                {
                    var a = m_Wireframe[0, iLat] * min;
                    GL.Vertex3(a.x, a.y, a.z);
                    var b = m_Wireframe[0, iLat] * max;
                    GL.Vertex3(b.x, b.y, b.z);
                }
                GL.End();

                GL.Begin(GL.LINES);
                GL.Color(s_WireColorB);
                for (int iLat = 0; iLat <= nLat; iLat++)
                {
                    var a = m_Wireframe[nLon, iLat] * min;
                    GL.Vertex3(a.x, a.y, a.z);
                    var b = m_Wireframe[nLon, iLat] * max;
                    GL.Vertex3(b.x, b.y, b.z);
                }
                GL.End();
            }

            GL.PopMatrix();
        }
    }
}
#endif
