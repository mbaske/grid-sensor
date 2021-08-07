#if (UNITY_EDITOR)
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using System;
using System.Linq;
using System.Collections;
using MBaske.Sensors.Util;

namespace MBaske.Sensors.Grid
{
    /// Serializable field whose PropertyDrawer visualizes the 
    /// <see cref="GridBuffer"/> contents and channel labels at runtime.
    /// </summary>
    // TODO Show/hide/repaint logic should be clearer. 
    // Problem is we need to repaint the drawer when grid buffer contents change
    // and therefore can't rely on the Unity editor triggering OnGUI calls.
    // Workaround is getting the drawer's Editor instance and manually forcing
    // it to repaint on sensor update events. This is expensive though and 
    // forcing repaints must be suspended when the drawer isn't visible. Which 
    // is done via delayed resetting of the m_Repaint flag by the coroutine.
    // Hacking the PropertyDrawer to do something it wasn't supposed to maybe
    // isn't the best idea in the first place.
    [Serializable]
    public class GridBufferDrawer
    {
        public DebugChannelData ChannelData;
        public GridBuffer Buffer;
        public MonoBehaviour Context;
        public Editor Editor;
        public float GridSizeRatio;
        public bool IsEnabled;
        public bool IsStandby;

        private bool m_Repaint;
        private bool m_RepaintOnFirstUpdate;
        private IEnumerator m_StopRepaintCoroutine;
        private readonly YieldInstruction m_StopRepaintDelay = new WaitForSeconds(0.5f);

        /// <summary>
        /// Enables drawer.
        /// </summary>
        /// <param name="channelData"><see cref="DebugChannelData"/> instance</param>
        /// <param name="buffer"><see cref="GridBuffer"/> instance</param>
        public void Enable(MonoBehaviour context, DebugChannelData channelData, GridBuffer buffer)
        {
            ChannelData = channelData;
            Buffer = buffer;
            GridSizeRatio = buffer.Height / (float)buffer.Width;

            Context = context;
            m_RepaintOnFirstUpdate = true;
            m_StopRepaintCoroutine ??= StopRepaintCoroutine();

            IsEnabled = true;
            IsStandby = false;
        }

        /// <summary>
        /// Standby disables grid draw, but keeps inspector height. 
        /// </summary>
        public void Standby()
        {
            IsEnabled = false;
            IsStandby = true;
        }

        /// <summary>
        /// Disables drawer.
        /// </summary>
        public void Disable()
        {
            IsEnabled = false;
            IsStandby = false;
            m_RepaintOnFirstUpdate = false;
        }

        /// <summary>
        /// Repaints drawer on sensor update.
        /// </summary>
        public void OnSensorUpdate()
        {
            if (m_RepaintOnFirstUpdate)
            {
                EnableRepaint();
            }

            if (m_Repaint && Editor != null)
            {
                Editor.Repaint();
            }
        }

        /// <summary>
        /// Sets the repaint flag and resets it after 0.5 secs.
        /// </summary>
        public void EnableRepaint()
        {
            if (Context != null)
            {
                CoroutineUtil.Start(Context, m_StopRepaintCoroutine);
                m_Repaint = true;
            }
        }

        private IEnumerator StopRepaintCoroutine()
        {
            yield return m_StopRepaintDelay;
            m_Repaint = false;
        }
    }

    [CustomPropertyDrawer(typeof(GridBufferDrawer))]
    public class GridBufferDrawerGUI : PropertyDrawer
    {
        private static readonly GUIStyle s_Style = new GUIStyle()
        {
            richText = true,
            fontStyle = FontStyle.Bold
        };

        private static readonly GUIContent s_Msg = new GUIContent(
                    "<color=#CC6666>Available in Game Play Mode</color>");

        private static readonly Color s_BGColor = new Color32(32, 32, 32, 255);

        private GridBufferDrawer m_Target;
        private NativeArray<Color32> m_Pixels;
        private Texture2D m_Texture;
        private Color32[] m_Black;
        private Rect m_GridRect;
        private Rect m_FullRect;
        private bool[] m_DrawChannel;


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return m_FullRect.height; 
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            m_Target ??= fieldInfo.GetValue(property.serializedObject.targetObject) 
                as GridBufferDrawer;

            if (m_Target.Editor == null)
            {
                // Becomes null when selected gameobject is changed in hierarchy.
                m_Target.Editor = EditorUtil.GetEditor(property.serializedObject);
            }

            if (Application.isPlaying && (m_Target.IsEnabled || m_Target.IsStandby))
            {
                m_Target.EnableRepaint();
            }
            else
            {
                return;
            }

            bool draw = Event.current.type == EventType.Repaint;
     
            if (m_Target.IsEnabled)
            {
                if (draw)
                {
                    CalcRects(rect);
                    ValidateTexture(m_Target.Buffer.Width, m_Target.Buffer.Height);

                    if (m_Target.ChannelData.HasGridPositions)
                    {
                        // Draws occupied grid positions only.
                        DrawGL(DrawBackground, DrawPartialGrid);
                    }
                    else
                    {
                        // Draws entire grid.
                        DrawGL(DrawBackground, DrawFullGrid);
                    }

                    EditorGUI.DrawPreviewTexture(m_GridRect, m_Texture);
                }

                DrawLabels();
            }
            else if (m_Target.IsStandby)
            {
                // Is disabled during inspector value change.
                if (draw)
                {
                    // Empty, keep previous size.
                    DrawGL(DrawBackground);
                }
            }
            else
            {
                // Is edit mode, show message.
                m_FullRect.height = 16;
                rect.x += 2;
                rect.y += 1;
                EditorGUI.LabelField(rect, s_Msg, s_Style);
            }
        }

        private void CalcRects(Rect rect)
        {
            float width = EditorGUIUtility.currentViewWidth;

            m_GridRect = rect;
            m_GridRect.x = EditorGUIUtility.labelWidth + 20;
            m_GridRect.width = width - m_GridRect.x;
            m_GridRect.height = m_GridRect.width * m_Target.GridSizeRatio;

            m_FullRect = rect;
            m_FullRect.x = 0;
            m_FullRect.width = width;
            int reqTextHeight = m_Target.Buffer.NumChannels * 16 + 6;
            m_FullRect.height = Mathf.Max(reqTextHeight, m_GridRect.height);
        }

        private void ValidateTexture(int w, int h)
        {
            if (m_Texture == null || m_Texture.width != w || m_Texture.height != h)
            {
                m_Texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };
                m_Pixels = m_Texture.GetRawTextureData<Color32>();
                m_Black = Enumerable.Repeat(new Color32(0, 0, 0, 255), w * h).ToArray();
            }
        }

        private void DrawGL(params Action[] jobs)
        {
            GUI.BeginClip(m_FullRect);
            GL.PushMatrix();
            GL.Clear(true, false, Color.black);
            EditorUtil.GLMaterial.SetPass(0);

            foreach (var job in jobs)
            {
                job.Invoke();
            }

            GL.PopMatrix();
            GUI.EndClip();
        }

        private void DrawBackground()
        {
            // Full
            GL.Begin(GL.QUADS);
            GL.Color(s_BGColor);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(m_FullRect.width, 0, 0);
            GL.Vertex3(m_FullRect.width, m_FullRect.height, 0);
            GL.Vertex3(0, m_FullRect.height, 0);
            GL.End();
            // Grid
            GL.Begin(GL.QUADS);
            GL.Color(Color.black);
            GL.Vertex3(m_GridRect.x, 0, 0);
            GL.Vertex3(m_FullRect.width, 0, 0);
            GL.Vertex3(m_FullRect.width, m_FullRect.height, 0);
            GL.Vertex3(m_GridRect.x, m_FullRect.height, 0);
            GL.End();
        }

        private void DrawPartialGrid()
        {
            var channelData = m_Target.ChannelData;
            var buffer = m_Target.Buffer;
            int n = buffer.NumChannels;
            int w = buffer.Width;

            m_Pixels.CopyFrom(m_Black);

            for (int c = 0; c < n; c++)
            {
                if (m_DrawChannel[c])
                {
                    Color color = channelData.GetColor(c);
                    var positions = channelData.GetGridPositions(c);

                    foreach (var pos in positions)
                    {
                        int i = pos.y * w + pos.x;
                        Color32 a = m_Pixels[i];
                        Color32 b = buffer.Read(c, pos) * color;
                        // Add colors.
                        b.r = (byte)Math.Min(a.r + b.r, 255);
                        b.g = (byte)Math.Min(a.g + b.g, 255);
                        b.b = (byte)Math.Min(a.b + b.b, 255);
                        b.a = 255;
                        m_Pixels[i] = b;
                    }
                }
            }

            m_Texture.Apply();
        }

        private void DrawFullGrid()
        {
            var channelData = m_Target.ChannelData;
            var buffer = m_Target.Buffer;
            int n = buffer.NumChannels;
            int w = buffer.Width;
            int h = buffer.Height;

            m_Pixels.CopyFrom(m_Black);

            for (int c = 0; c < n; c++)
            {
                if (m_DrawChannel[c])
                {
                    Color color = channelData.GetColor(c);

                    for (int x = 0; x < w; x++)
                    {
                        for (int y = 0; y < h; y++)
                        {
                            int i = y * w + x;
                            Color32 a = m_Pixels[i];
                            Color32 b = buffer.Read(c, x, y) * color;
                            // Add colors.
                            b.r = (byte)Math.Min(a.r + b.r, 255);
                            b.g = (byte)Math.Min(a.g + b.g, 255);
                            b.b = (byte)Math.Min(a.b + b.b, 255);
                            b.a = 255;
                            m_Pixels[i] = b;
                        }
                    }
                }
            }

            m_Texture.Apply();
        }

        private void DrawLabels()
        {
            var channelData = m_Target.ChannelData;
            int n = m_Target.Buffer.NumChannels;

            Rect rect = m_FullRect;
            rect.x = 2;
            rect.y += 2;
            rect.height = 16;

            if (m_DrawChannel == null || m_DrawChannel.Length != n)
            {
                m_DrawChannel = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    m_DrawChannel[i] = true;
                }
            }

            for (int i = 0; i < n; i++)
            {
                bool draw = m_DrawChannel[i];
                string rbg = ColorUtility.ToHtmlStringRGB(
                    draw ? channelData.GetColor(i) : Color.grey);
                m_DrawChannel[i] = EditorGUI.ToggleLeft(rect, 
                    $"<color=#{rbg}>{channelData.GetChannelName(i)}</color>",
                    draw, s_Style);
                rect.y += 16;
            }
        }
    }
}
#endif