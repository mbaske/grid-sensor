using System.Collections.Generic;
using UnityEngine;

namespace MBaske.Driver
{
    public class Road : MonoBehaviour
    {
        public Transform FirstChunkTF => m_Chunks.Peek().transform;

        [SerializeField]
        private int numChunks = 20;

        [SerializeField]
        private ReferenceFrame m_Frame;
        private Queue<RoadChunk> m_Chunks;
        private RoadChunkPool m_Pool;

        public void Initialize()
        {
            m_Chunks = new Queue<RoadChunk>(numChunks);
            m_Pool = FindObjectOfType<RoadChunkPool>();
        }

        public void ManagedReset()
        {
            m_Frame.ManagedReset();

            while (m_Chunks.Count > 0)
            {
                m_Chunks.Dequeue().Discard();
            }
            while (m_Chunks.Count < numChunks)
            {
                EnqueuNewChunk();
            }
        }

        public void ReplaceFirstChunk()
        {
            m_Chunks.Dequeue().Discard();
            EnqueuNewChunk();
        }

        private void EnqueuNewChunk()
        {
            var chunk = (RoadChunk)m_Pool.Spawn(m_Frame.transform.position);
            chunk.UpdateChunk(m_Frame, m_Chunks.Count == 0);
            m_Chunks.Enqueue(chunk);
        }
    }
}