#if (UNITY_EDITOR)
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Scene GUI editor for <see cref="GridSensorComponent2D"/>.
    /// Attached to <see cref="GridEditorHelper2D"/>.
    /// </summary>
    [CustomEditor(typeof(GridEditorHelper2D)), CanEditMultipleObjects]
    public class GridEditor2D : Editor
    {
        private BoxBoundsHandle m_BoundsHandle;
        private GridSensorComponent2D m_Comp;

        private static readonly Color s_GridColor = new Color(0.1f, 0.4f, 1, 1);

        private void OnEnable()
        {
            m_BoundsHandle = new BoxBoundsHandle();
            m_BoundsHandle.SetColor(Color.white);
        }

        private void OnSceneGUI()
        {
            m_Comp ??= ((GridEditorHelper2D)target).GetSensorComponent();

            if (m_Comp)
            {
                DrawHandles();
                GetKeyInput();
            }
        }

        private void DrawHandles()
        {
            float cellSize = m_Comp.CellSize;
            Matrix4x4 matrix = Matrix4x4.TRS(m_Comp.transform.position, m_Comp.GetRotation(), Vector3.one);

            using (new Handles.DrawingScope(matrix))
            {
                EditorGUI.BeginChangeCheck();
                {
                    m_BoundsHandle.center = m_Comp.EditorBounds.center;
                    m_BoundsHandle.size = m_Comp.EditorBounds.size;
                    m_BoundsHandle.DrawHandle();

                    // Grid lines.

                    Bounds b = m_Comp.DetectionBounds;
                    Handles.color = s_GridColor;
                    Handles.DrawWireCube(b.center, b.size);

                    float y = b.center.y;
                    float xMin = b.min.x;
                    float xMax = b.max.x;
                    float zMin = b.min.z;
                    float zMax = b.max.z;

                    for (float x = xMin + cellSize, max = xMax - cellSize * 0.5f; x < max; x += cellSize)
                    {
                        Handles.DrawLine(new Vector3(x, y, zMin), new Vector3(x, y, zMax));
                    }
                    for (float z = zMin + cellSize, max = zMax - cellSize * 0.5f; z < max; z += cellSize)
                    {
                        Handles.DrawLine(new Vector3(xMin, y, z), new Vector3(xMax, y, z));
                    }
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Bounds b = new Bounds(m_BoundsHandle.center, m_BoundsHandle.size);
                    b.size = new Vector3(
                        Mathf.Max(b.size.x, cellSize),
                        Mathf.Max(b.size.y, 1),
                        Mathf.Max(b.size.z, cellSize));

                    m_Comp.EditorBounds = b;
                }
            }
        }

        private void GetKeyInput()
        {
            // Key commands:
            // S -> Snap to grid
            // C -> Center on X-axis
            // Shift + C -> Center on all axes

            Event e = Event.current;
            switch (e.type)
            {
                case EventType.KeyDown:
                    switch (e.keyCode)
                    {
                        case KeyCode.C:
                            m_Comp.CenterDetectionBoundsOnAxes(!e.shift);
                            m_Comp.EditorBounds = m_Comp.DetectionBounds;
                            break;
                        case KeyCode.S:
                            m_Comp.RoundDetectionBoundsCenter();
                            m_Comp.EditorBounds = m_Comp.DetectionBounds;
                            break;
                    }
                    break;
            }
        }
    }
}
#endif
