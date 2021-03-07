using UnityEngine;

namespace MBaske
{
	public class FPSDisplay : MonoBehaviour
	{
		[SerializeField]
		private int m_BufferSize = 60;
		private int m_Index;
		private float[] m_Buffer;
		private float m_DeltaTime;
		private GUIStyle m_Style;
		private Rect m_Rect;

		private void Awake()
		{
			m_Buffer = new float[m_BufferSize];
			m_Rect = new Rect(Screen.width - 110, 10, 100, 20);
			m_Style = new GUIStyle
			{
				alignment = TextAnchor.UpperRight
			};
			m_Style.normal.textColor = Color.white;
			m_Style.fontSize = 16;
		}

		private void Update()
		{
			m_Buffer[m_Index] = Time.unscaledDeltaTime;
			m_Index = ++m_Index % m_BufferSize;
			m_DeltaTime = 0;

			for (int i = 0; i < m_BufferSize; i++)
			{
				m_DeltaTime += m_Buffer[i];
			}
			m_DeltaTime /= (float)m_BufferSize;
		}

		private void OnGUI()
		{
			GUI.Label(m_Rect, string.Format("{0:0.0} fps", 1 / m_DeltaTime), m_Style);
		}
	}
}