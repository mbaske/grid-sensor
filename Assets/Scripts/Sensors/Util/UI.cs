
using UnityEngine;

namespace MBaske.Sensors.Util
{
    public static class UI
    {
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
    }
}