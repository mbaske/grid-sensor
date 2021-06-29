
using UnityEngine;

namespace MBaske.Driver
{
    /// <summary>
    /// Generates positions for chunks and their vertices.
    /// </summary>
    public class ReferenceFrame : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Multiplier for curvature.")]
        private float m_CurveStrength = 360;
        [SerializeField]
        [Tooltip("Curvature noise scale.")]
        private float m_CurveScale = 0.005f;
        
        [SerializeField]
        [Tooltip("Multiplier for slope.")]
        private float m_SlopeStrength = 60;
        [SerializeField]
        [Tooltip("Slope noise scale.")]
        private float m_SlopeScale = 0.005f;

        [SerializeField]
        private float m_NoiseBias = 0.035f;

        private int m_StepCount;
        private Vector2 m_CurveOffset;
        private Vector2 m_SlopeOffset;
        private Vector3 m_NextPos;
        private Vector3 m_DefPos;

        private void Awake()
        {
            m_DefPos = transform.position;
        }

        /// <summary>
        /// Resets the frame.
        /// </summary>
        public void ManagedReset()
        {
            m_StepCount = 0;
            transform.position = m_DefPos;
            transform.rotation = Quaternion.identity;
            m_CurveOffset = Random.insideUnitCircle * 10;
            m_SlopeOffset = Random.insideUnitCircle * 10;
            m_NextPos = NextPos();
            MoveFrame();
        }

        /// <summary>
        /// Moves the frame forward incrementally.
        /// </summary>
        /// <returns></returns>
        public Transform MoveFrame()
        {
            transform.position = m_NextPos;
            m_NextPos = NextPos();
            transform.LookAt(m_NextPos);
            m_StepCount++;

            return transform;
        }

        private Vector3 NextPos()
        {
            var pos = transform.position
                + Quaternion.AngleAxis(
                    Noise(m_StepCount * m_CurveScale, m_CurveOffset) * m_CurveStrength,
                    Vector3.up)
                * Vector3.forward;

            pos.y = m_DefPos.y + Noise(m_StepCount * m_SlopeScale, m_SlopeOffset) * m_SlopeStrength;

            return pos;
        }

        private float Noise(float x, Vector2 offset)
        {
            return (Mathf.PerlinNoise(x + offset.x, offset.y) - 0.5f) + m_NoiseBias;
        }
    }
}