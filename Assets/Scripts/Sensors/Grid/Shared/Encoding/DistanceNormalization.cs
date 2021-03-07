using UnityEngine;
#if (UNITY_EDITOR)
using UnityEditor;
using MBaske.Sensors.Util;
#endif

namespace MBaske.Sensors.Grid
{
    public enum DistanceNormalizationType
    {
        Linear, Weighted
    }

    [System.Serializable]
    public class DistanceNormalization
    {
        public float Value = 1;
        public bool Weighted;

        public float Evaluate(float norm)
        {
            // Original point.z normalization is 0 (near) to 1 (far).
            // We invert this here so that closer points have a higher
            // brightness on the grid.
            return Weighted ? InvSigmoid(norm, Value) : 1 - norm;
        }

        public static float InvSigmoid(float norm, float weight)
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
                UI.GLMaterial.SetPass(0);

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
                    float s = weighted ? DistanceNormalization.InvSigmoid(t, value) : 1 - t;
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