#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;

namespace MBaske.Sensors.Util
{
    public static class EditorUtil
    {
        /// <summary>
        /// The material used for drawing GL stuff.
        /// </summary>
        public static Material GLMaterial = CreateGLMaterial();

        private static Material CreateGLMaterial()
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            Material material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            // Turn on alpha blending.
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off.
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes.
            material.SetInt("_ZWrite", 0);
            return material;
        }

        /// <summary>
        /// Repaints an inspector drawer or editor.
        /// </summary>
        /// <param name="obj">The SerializedObject</param>
        public static void Repaint(SerializedObject obj)
        {
            GetEditor(obj).Repaint();
        }

        /// <summary>
        /// Returns the editor for a specified SerializedObject.
        /// Returned reference becomes null when selected 
        /// gameobject is changed in hierarchy.
        /// </summary>
        /// <param name="obj">The SerializedObject</param>
        /// <returns>Editor for SerializedObject</returns>
        public static Editor GetEditor(SerializedObject obj)
        {
            foreach (var editor in ActiveEditorTracker.sharedTracker.activeEditors)
            {
                if (editor.serializedObject == obj)
                {
                    return editor;
                }
            }

            throw new MissingComponentException("Editor not available for " + obj);
        }

        /// <summary>
        /// Hides the BehaviorParametersEditor inspector.
        /// </summary>
        // Workaround for https://github.com/Unity-Technologies/ml-agents/issues/5443
        public static void HideBehaviorParametersEditor()
        {
            var tracker = ActiveEditorTracker.sharedTracker;
            var editors = tracker.activeEditors;
            for (int i = 0; i < editors.Length; i++)
            {
                // Can't check type because BehaviorParametersEditor is internal.
                if (editors[i].ToString() == " (Unity.MLAgents.Editor.BehaviorParametersEditor)")
                {
                    tracker.SetVisible(i, 0);
                }
            }
        }
    }
}
#endif