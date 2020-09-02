using UnityEngine;

namespace MBaske
{
    /// <summary>
    /// Draws each <see cref="PixelGrid"/> layer into a GUI texture.
    /// </summary>
    [RequireComponent(typeof(IPixelGridProvider))]
    public class PixelGridVisualizer : MonoBehaviour
    {
        [SerializeField, Range(1, 16)]
        int m_Magnify = 8;

        PixelGrid m_Grid;
        Texture2D[] m_Textures;

        private void Initialize()
        {
            m_Textures = new Texture2D[m_Grid.NumLayers];
            for (int i = 0; i < m_Grid.NumLayers; i++)
            {
                m_Textures[i] = new Texture2D(m_Grid.Width, m_Grid.Width, TextureFormat.RGB24, false);
                m_Textures[i].filterMode = FilterMode.Point;
                m_Textures[i].wrapMode = TextureWrapMode.Clamp;
            }
        }

        private void OnGUI()
        {
            if (m_Grid == null)
            {
                m_Grid = GetComponent<IPixelGridProvider>().GetPixelGrid();
                Initialize();
            }

            int i = 0;
            // Spacing equals magnification factor.
            Rect rect = new Rect(m_Magnify, m_Magnify,
                m_Grid.Width * m_Magnify, m_Grid.Height * m_Magnify);

            foreach (Color32[] colors in m_Grid.GetColors())
            {
                m_Textures[i].SetPixels32(colors);
                m_Textures[i].Apply();
                GUI.DrawTexture(rect, m_Textures[i]);
                rect.x += (m_Grid.Width + 1) * m_Magnify;
                i++;
            }
        }
    }
}
