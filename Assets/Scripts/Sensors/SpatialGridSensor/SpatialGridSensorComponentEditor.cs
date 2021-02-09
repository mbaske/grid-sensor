#if (UNITY_EDITOR)
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MBaske.Sensors.Util;

namespace MBaske.Sensors
{
    [CustomEditor(typeof(SpatialGridSensorComponent))]
    public class SpatialGridSensorComponentEditor : Editor
    {
        private SpatialGridSensorComponent m_Comp;
        private bool m_UpdateFlag;

        private List<int> m_Layers;
        private List<string> m_Tags;

        private ArcHandle m_ArcHandleLatN;
        private ArcHandle m_ArcHandleLatS;
        private ArcHandle m_ArcHandleLon;

        private static Color m_WireColorA = new Color(0f, 0.5f, 1f, 0.3f);
        private static Color m_WireColorB = new Color(0f, 0.5f, 1f, 0.1f);
        private static Color m_CurveColor = new Color(0f, 0.5f, 1f, 1f);
        private static Material m_GLMaterial;

        protected virtual void OnEnable()
        {
            m_Comp = (SpatialGridSensorComponent)target;
            Undo.undoRedoPerformed += OnUndoRedo;
            m_UpdateFlag = true;

            m_ArcHandleLatN = new ArcHandle();
            m_ArcHandleLatN.SetColorWithoutRadiusHandle(new Color32(152, 237, 67, 255), 0.1f);
            m_ArcHandleLatN.radiusHandleColor = Color.white;
            
            m_ArcHandleLatS = new ArcHandle();
            m_ArcHandleLatS.SetColorWithoutRadiusHandle(new Color32(152, 237, 67, 255), 0.1f);
            m_ArcHandleLatS.radiusHandleColor = Color.white;
           
            m_ArcHandleLon = new ArcHandle();
            m_ArcHandleLon.SetColorWithoutRadiusHandle(new Color32(237, 67, 30, 255), 0.1f);
            m_ArcHandleLon.radiusHandleColor = Color.white;

            CreateGLMaterial();
        }

        private void RecordUpdate()
        {
            Undo.RecordObject(m_Comp, "Gridsensor Update");
        }

        private void OnUndoRedo()
        {
            // TODO target is null here after undo in playmode?
            m_UpdateFlag = true;
        }

        private void CheckFlags()
        {
            if (m_UpdateFlag || m_Comp.ResetFlag)
            {
                m_Comp.UpdateSensor();

                m_Layers = new List<int>(m_Comp.Layers);
                m_Tags = new List<string>(m_Comp.Tags);

                m_Comp.ResetFlag = false;
                m_UpdateFlag = false;
            }
        }

        public override void OnInspectorGUI()
        {
            CheckFlags();

            var so = serializedObject;
            so.Update();

            EditorGUI.BeginChangeCheck();
            {
                // GENERAL SETTINGS

                EditorGUILayout.PropertyField(so.FindProperty("m_SensorName"));
                EditorGUILayout.PropertyField(so.FindProperty("m_BufferSize"));
                EditorGUILayout.PropertyField(so.FindProperty("m_ObservationStackSize"));
                EditorGUILayout.PropertyField(so.FindProperty("m_CompressionType"));
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Observation Shape", GUILayout.Width(EditorGUIUtility.labelWidth));
                    EditorGUILayout.LabelField(m_Comp.GetGridShape().ToString());
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();

                // DETECTABLE LAYERS & TAGS

                EditorGUILayout.BeginHorizontal();
                {
                    m_Layers[0] = EditorGUILayout.LayerField("Layers", m_Layers[0]);
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        m_Layers.Add(0);
                    }
                }
                EditorGUILayout.EndHorizontal();

                for (int i = 1; i < m_Layers.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        m_Layers[i] = EditorGUILayout.LayerField("\t", m_Layers[i]);
                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            m_Layers.RemoveAt(i);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                {
                    m_Tags[0] = EditorGUILayout.TagField("Tags", m_Tags[0]);
                    if (GUILayout.Button("+", GUILayout.Width(20)))
                    {
                        m_Tags.Add("Untagged");
                    }
                }
                EditorGUILayout.EndHorizontal();

                for (int i = 1; i < m_Tags.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        m_Tags[i] = EditorGUILayout.TagField("\t", m_Tags[i]);
                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            m_Tags.RemoveAt(i);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                // ENCODING & DETECTION

                EditorGUILayout.PropertyField(so.FindProperty("m_ChannelEncoding"));
                EditorGUILayout.PropertyField(so.FindProperty("m_DetectionType"));

                // Stats
                if (m_Comp.HasDetectionStats(out string stats))
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Detection Stats", GUILayout.Width(EditorGUIUtility.labelWidth));
                        EditorGUILayout.LabelField(stats);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                // Scan
                if (m_Comp.DetectionType == ColliderDetectionType.Shape)
                {
                    EditorGUILayout.Slider(so.FindProperty("m_ScanResolution"), 0.25f, 5f);
                    EditorGUILayout.Slider(so.FindProperty("m_ScanExtent"), 1, 25);
                    EditorGUILayout.PropertyField(so.FindProperty("m_ClearCacheOnReset"));
                }

                // Blur
                EditorGUILayout.Slider(so.FindProperty("m_BlurStrength"), 0, 2);
                EditorGUI.BeginDisabledGroup(m_Comp.BlurStrength == 0);
                {
                    EditorGUILayout.Slider(so.FindProperty("m_BlurThreshold"), 0, 1);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space();


                // GEOMETRY

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Resolution", GUILayout.Width(EditorGUIUtility.labelWidth));
                    EditorGUILayout.LabelField(ResolutionInfo());
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Slider(so.FindProperty("m_CellArc"), 0.25f, 9);
                EditorGUILayout.Slider(so.FindProperty("m_LatAngleNorth"), 0, 90);
                EditorGUILayout.Slider(so.FindProperty("m_LatAngleSouth"), 0, 90);
                EditorGUILayout.Slider(so.FindProperty("m_LonAngle"), 0, 180);
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(so.FindProperty("m_MinDistance"));
                EditorGUILayout.PropertyField(so.FindProperty("m_MaxDistance"));

                // DISTANCE NORMALIZATION

                EditorGUILayout.PropertyField(so.FindProperty("m_DistanceNormalization"));

                if (m_Comp.DistanceNormalization == DistanceNormalization.Weighted)
                {
                    EditorGUILayout.Slider(so.FindProperty("m_NormalizationWeight"), 0.01f, 1f);
                    DrawNormalizationCurve();
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (!m_Layers.SequenceEqual(m_Comp.Layers))
                {
                    RecordUpdate();
                    m_Comp.Layers = m_Layers.ToArray();
                }
                else if (!m_Tags.SequenceEqual(m_Comp.Tags))
                {
                    RecordUpdate();
                    m_Comp.Tags = m_Tags.ToArray();
                }
                else
                {
                    so.ApplyModifiedProperties();
                    m_Comp.UpdateSensor();
                }
            }
        }


        private void OnSceneGUI()
        {
            CheckFlags();
            DrawHandles();
            DrawWireFrame();
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
                    RecordUpdate();

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

            fwd = tf.forward;
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
                    RecordUpdate();

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

            fwd = tf.forward;
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
                    RecordUpdate();

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
                RecordUpdate();
                m_Comp.MinDistance = min;
            }
        }

        private string ResolutionInfo()
        {
            return string.Format("{0} meters ({1} deg at {2} meters)",
                string.Format("{0:0.00}", m_Comp.GridResolution),
                string.Format("{0:0.00}", m_Comp.CellArc),
                string.Format("{0:0.00}", m_Comp.MaxDistance));
        }

        private void DrawWireFrame()
        {
            int nLon = m_Comp.GridDimensions.x;
            int nLat = m_Comp.GridDimensions.y;
            Quaternion[,] wf = m_Comp.Wireframe;

            if ((nLon + 1) * (nLat + 1) != wf.Length)
            {
                return;
            }

            m_GLMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(m_Comp.transform.localToWorldMatrix);

            Vector3 min = Vector3.forward * m_Comp.MinDistance;
            Vector3 max = Vector3.forward * m_Comp.MaxDistance;

            // Grid Cells

            for (int iLat = 0; iLat <= nLat; iLat++)
            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(m_WireColorA);

                for (int iLon = 0; iLon <= nLon; iLon++)
                {
                    var v = wf[iLat, iLon] * max;
                    GL.Vertex3(v.x, v.y, v.z);
                }
                GL.End();
            }

            for (int iLon = 0; iLon <= nLon; iLon++)
            {
                GL.Begin(GL.LINE_STRIP);
                GL.Color(m_WireColorA);

                for (int iLat = 0; iLat <= nLat; iLat++)
                {
                    var v = wf[iLat, iLon] * max;
                    GL.Vertex3(v.x, v.y, v.z);
                }
                GL.End();
            }

            // Angles

            if (m_Comp.LatAngleSouth < 90)
            {
                GL.Begin(GL.LINES);
                GL.Color(m_WireColorB);

                for (int iLon = 0; iLon <= nLon; iLon++)
                {
                    var a = wf[0, iLon] * min;
                    GL.Vertex3(a.x, a.y, a.z);
                    var b = wf[0, iLon] * max;
                    GL.Vertex3(b.x, b.y, b.z);
                }
                GL.End();
            }

            if (m_Comp.LatAngleNorth < 90)
            {
                GL.Begin(GL.LINES);
                GL.Color(m_WireColorB);

                for (int iLon = 0; iLon <= nLon; iLon++)
                {
                    var a = wf[nLat, iLon] * min;
                    GL.Vertex3(a.x, a.y, a.z);
                    var b = wf[nLat, iLon] * max;
                    GL.Vertex3(b.x, b.y, b.z);
                }
                GL.End();
            }

            if (m_Comp.LonAngle < 180)
            {
                for (int iLon = 0; iLon <= nLon; iLon += nLon)
                {
                    GL.Begin(GL.LINES);
                    GL.Color(m_WireColorB);

                    for (int iLat = 0; iLat <= nLat; iLat++)
                    {
                        var a = wf[iLat, iLon] * min;
                        GL.Vertex3(a.x, a.y, a.z);
                        var b = wf[iLat, iLon] * max;
                        GL.Vertex3(b.x, b.y, b.z);
                    }
                    GL.End();
                }
            }

            GL.PopMatrix();
        }

        private void DrawNormalizationCurve()
        {
            Rect rect = GUILayoutUtility.GetRect(10, 1000, 50, 50);
  
            int w = Mathf.CeilToInt(rect.width / 50);
            float y = rect.height / 2;

            if (Event.current.type == EventType.Repaint)
            {
                GUI.BeginClip(rect);
                GL.PushMatrix();

                GL.Clear(true, false, Color.black);
                m_GLMaterial.SetPass(0);

                GL.Begin(GL.QUADS);
                GL.Color(Color.black);
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(rect.width, 0, 0);
                GL.Vertex3(rect.width, rect.height, 0);
                GL.Vertex3(0, rect.height, 0);
                GL.End();
                
                float weight = m_Comp.NormalizationWeight;

                GL.Begin(GL.LINES);
                GL.Color(m_CurveColor);
                for (int x = 0; x <= rect.width; x++)
                {
                    float t = x / rect.width;
                    float s = Normalization.InvSigmoid(t, weight);
                    GL.Vertex3(x, rect.height, 0);
                    GL.Vertex3(x, rect.height - s * rect.height, 0);
                }

                GL.Color(Color.grey);
                for (int i = 1; i < w; i++)
                {
                    float t = i / (float)w;
                    float x = Mathf.Lerp(0, rect.width, t);
                    GL.Vertex3(x, y, 0);
                    GL.Vertex3(x, y - 5, 0);
                    
                }
                GL.End();

                GL.PopMatrix();
                GUI.EndClip();
            }

            for (int i = 1; i < w; i++)
            {
                float t = i / (float)w;
                float x = Mathf.Lerp(0, rect.width, t);
                float d = Mathf.Lerp(m_Comp.MinDistance, m_Comp.MaxDistance, t);
                EditorGUI.LabelField(new Rect(x + 4, rect.yMax - y, 80, 20), string.Format("{0:0.00}", d));
            }
        }

        private static void CreateGLMaterial()
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            m_GLMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            // Turn on alpha blending.
            m_GLMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m_GLMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off.
            m_GLMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes.
            m_GLMaterial.SetInt("_ZWrite", 0);
        }
    }
}
#endif