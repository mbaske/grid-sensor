using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Driver
{
    /// <summary>
    /// The road is composed of <see cref="RoadChunk"/>s.
    /// </summary>
    public class Road : MonoBehaviour
    {
        /// <summary>
        /// Reference to first <see cref="RoadChunk"/>s transform.
        /// </summary>
        public Transform FirstChunkTF => m_Chunks.Peek().transform;

        [SerializeField]
        private int m_NumChunks = 20;

        [SerializeField]
        private ReferenceFrame m_Frame;
        private Queue<RoadChunk> m_Chunks;
        private RoadChunkPool m_Pool;

        /// <summary>
        /// Initializes the road.
        /// </summary>
        public void Initialize()
        {
            m_Chunks = new Queue<RoadChunk>(m_NumChunks);
            m_Pool = FindObjectOfType<RoadChunkPool>();
        }

        /// <summary>
        /// Resets the road.
        /// </summary>
        public void ManagedReset()
        {
            m_Frame.ManagedReset();

            while (m_Chunks.Count > 0)
            {
                m_Chunks.Dequeue().Discard();
            }
            while (m_Chunks.Count < m_NumChunks)
            {
                EnqueuNewChunk();
            }
        }

        /// <summary>
        /// Removes the first (oldest) <see cref="RoadChunk"/>
        /// and creates a new one at the end of the road.
        /// </summary>
        public void ReplaceFirstChunk()
        {
            m_Chunks.Dequeue().Discard();
            EnqueuNewChunk();
        }

        private void EnqueuNewChunk()
        {
            var chunk = m_Pool.Spawn(m_Frame.transform.position);
            chunk.UpdateChunk(m_Frame, m_Chunks.Count == 0);
            m_Chunks.Enqueue(chunk);
        }
    }
}