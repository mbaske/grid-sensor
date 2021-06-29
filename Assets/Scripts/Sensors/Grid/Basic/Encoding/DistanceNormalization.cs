using UnityEngine;
#if (UNITY_EDITOR)
using UnityEditor;
using MBaske.Sensors.Util;
#endif

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// How distance values are normalized, linear or weighted.
    /// </summary>
    public enum DistanceNormalizationType
    {
        Linear, Weighted
    }

    /// <summary>
    /// Specifies distance value normalization.
    /// </summary>
    [System.Serializable]
    public class DistanceNormalization
    {
        /// <summary>
        /// Curvature value.
        /// </summary>
        public float Value = 1;
        /// <summary>
        /// Whether normalization is weighted (Value < 1).
        /// </summary>
        public bool Weighted;

        /// <summary>
        /// Evaluates and inverts normalized distance.
        /// Input is already normalized 0 (near) -> 1 (far),
        /// curvature is applied if <see cref="Value"/> < 1.
        /// Invert to 0 (far) -> 1 (near), so that closer
        /// points are brighter for visual observations.
        /// </summary>
        /// <param name="normDistance">Normalized distance</param>
        /// <returns>Inverted normalized distance (proximity)</returns>
        public float Evaluate(float normDistance)
        {
            return Weighted ? Sigmoid(normDistance, Value) : 1 - normDistance;
        }

        public static float Sigmoid(float norm, float weight)
        {
            return 1 - norm / (weight + Mathf.Abs(norm)) * (weight + 1);
        }
    }


#if (UNITY_EDITOR)
    [CustomPropertyDrawer(typeof(DistanceNormalization))]
    public class DistanceNormalizationDrawer : PropertyDrawer
    {
        private static readonly Color s_BGColor = new Color32(41, 41, 41, 255);
        private static readonly Color s_CurveColor = new Color(0f, 0.35f, 0.75f, 1f);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 60;

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            rect.height = 20;
            var prop = property.FindPropertyRelative("Value");
            EditorGUI.Slider(rect, prop, 0.01f, 1, label);
            float value = prop.floatValue;
            bool weighted = value < 1;
            property.FindPropertyRelative("Weighted").boolValue = weighted;

            if (Event.current.type == EventType.Repaint)
            {
                rect.y += 22;
                rect.height = 30;

                GUI.BeginClip(rect);
                GL.PushMatrix();

                GL.Clear(true, false, Color.black);
                EditorUtil.GLMaterial.SetPass(0);

                GL.Begin(GL.QUADS);
                GL.Color(s_BGColor);
                GL.Vertex3(0, 0, 0);
                GL.Vertex3(rect.width, 0, 0);
                GL.Vertex3(rect.width, rect.height, 0);
                GL.Vertex3(0, rect.height, 0);
                GL.End();

                GL.Begin(GL.LINES);
                GL.Color(s_CurveColor);
                for (int x = 0; x <= rect.width; x++)
                {
                    float t = x / rect.width;
                    float s = weighted ? DistanceNormalization.Sigmoid(t, value) : 1 - t;
                    GL.Vertex3(x, rect.height, 0);
                    GL.Vertex3(x, rect.height - s * rect.height, 0);
                }
                GL.End();

                GL.PopMatrix();
                GUI.EndClip();
            }

            rect.x += 4;
            EditorGUI.LabelField(rect, "Near");
            rect.x = rect.width - 8;
            EditorGUI.LabelField(rect, "Far");
        }
    }
#endif
}