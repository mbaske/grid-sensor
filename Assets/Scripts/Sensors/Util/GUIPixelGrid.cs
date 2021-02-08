using UnityEngine;

namespace MBaske.Sensors.Util
{
    /// <summary>
    /// Draws each <see cref="PixelGrid"/> layer into a GUI texture.
    /// </summary>
    public class GUIPixelGrid : MonoBehaviour
    {
        [SerializeField, Range(1, 16)]
        private int m_Magnify = 8;
        [SerializeField]
        private Vector2Int m_Offset;
        [SerializeField]
        private int m_Spacing = 2;
        [SerializeField]
        private string m_Label;
        [SerializeField]
        private Vector2Int m_Margins = new Vector2Int(3, 1);

        private IPixelGridProvider m_GridProvider;
        private PixelGrid Grid => m_GridProvider.GetPixelGrid();
        private bool GridIsInvalid => m_Grid != Grid;

        private PixelGrid m_Grid;
        private Texture2D[] m_Textures;
        private GUIStyle m_Style;

        private void Awake()
        {
            m_GridProvider = GetComponent<IPixelGridProvider>();
            m_Style = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft
            };
            m_Style.normal.textColor = Color.white;
            m_Style.fontSize = 12;
        }

        private void CreateTextures()
        {
            m_Grid = Grid;
            m_Textures = new Texture2D[m_Grid.Layers];

            for (int layer = 0; layer < m_Grid.Layers; layer++)
            {
                m_Textures[layer] = new Texture2D(m_Grid.Width, m_Grid.Height, TextureFormat.RGB24, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
        }

        private void OnGUI()
        {
            if (GridIsInvalid)
            {
                CreateTextures();
            }

            Rect rect = new Rect(m_Offset, m_Grid.Dimensions * m_Magnify);

            int layer = 0;
            foreach (Color32[] colors in m_Grid.GetColors())
            {
                m_Textures[layer].SetPixels32(colors);
                m_Textures[layer].Apply();

                Rect flipped = rect;// texture is upside down
                flipped.y += flipped.height;
                flipped.height *= -1; 
                GUI.DrawTexture(flipped, m_Textures[layer]);
                layer++;

                Rect label = rect;
                label.min += m_Margins;
                GUI.Label(label, m_Label, m_Style);

                rect.y += m_Grid.Height * m_Magnify + m_Spacing;
            }
        }
    }
}