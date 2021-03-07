using UnityEngine;

namespace MBaske.Sensors.Grid.Util
{
    /// <summary>
    /// Draws each <see cref="PixelGrid"/> layer into a GUI texture.
    /// </summary>
    public class GridSensorGUI : MonoBehaviour
    {
        [SerializeField]
        private MonoBehaviour m_PixelGridProvider;
        private PixelGrid m_Grid;
        private Texture2D[] m_Textures;

        [SerializeField, Range(1, 16)]
        private int m_Magnify = 8;
        [Space, SerializeField]
        private int m_Spacing = 2;
        [SerializeField]
        private Vector2Int m_Offset;

        [Space, SerializeField]
        private string m_Label;
        [SerializeField]
        private Vector2Int m_Margins = new Vector2Int(3, 1);
        private GUIStyle m_Style;

        private const int c_CheckValidInterval = 60;
        private int m_StepCount;


        private PixelGrid GetGrid() => ((IPixelGridProvider)m_PixelGridProvider).GetPixelGrid();

        private bool GridIsInvalid(out PixelGrid updatedGrid)
        {
            updatedGrid = GetGrid();
            return updatedGrid != m_Grid;
        }

        private void Reset()
        {
            if (TryGetComponent(out IPixelGridProvider provider))
            {
                m_PixelGridProvider = (MonoBehaviour)provider;
                if (provider is GridSensorComponentBase)
                {
                    m_Label = ((GridSensorComponentBase)provider).SensorName;
                }
            }
        }

        private void OnValidate()
        {
            if (m_PixelGridProvider != null && !(m_PixelGridProvider is IPixelGridProvider))
            {
                Debug.LogError("Referenced MonoBehaviour doesn't implement IPixelGridProvider");
            }
        }

        private void Awake()
        {
            m_Style = new GUIStyle
            {
                alignment = TextAnchor.UpperLeft
            };
            m_Style.normal.textColor = Color.white;
            m_Style.fontSize = 12;

            InitTextures(GetGrid());
        }

        private void InitTextures(PixelGrid grid)
        {
            m_Grid = grid;
            m_Textures = new Texture2D[m_Grid.Layers];

            for (int i = 0, n = m_Grid.Layers; i < n; i++)
            {
                m_Textures[i] = new Texture2D(m_Grid.Width, m_Grid.Height, TextureFormat.RGB24, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
        }

        private void OnGUI()
        {
            if (++m_StepCount % c_CheckValidInterval == 0)
            {
                if (GridIsInvalid(out PixelGrid grid))
                {
                    InitTextures(grid);
                }
                m_StepCount = 0;
            }

            Rect rect = new Rect(m_Offset, m_Grid.Size * m_Magnify);

            var colors = m_Grid.LayerColors;
            for (int i = 0, n = colors.Length; i < n; i++)
            {
                m_Textures[i].SetPixels32(colors[i]);
                m_Textures[i].Apply();

                Rect flipped = rect;// texture is upside down
                flipped.y += flipped.height;
                flipped.height *= -1; 
                GUI.DrawTexture(flipped, m_Textures[i]);

                Rect label = rect;
                label.min += m_Margins;
                GUI.Label(label, m_Label, m_Style);

                rect.y += m_Grid.Height * m_Magnify + m_Spacing;
            }
        }
    }
}