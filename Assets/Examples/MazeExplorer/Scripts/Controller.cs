using MBaske.Sensors.Grid;
using UnityEngine;

namespace MBaske.MazeExplorer
{
    /// <summary>
    /// Coordinates <see cref="MazeAgent"/> and <see cref="Maze"/>.
    /// Generates <see cref="Maze"/>'s <see cref="GridBuffer"/> according to settings.
    /// </summary>
    public class Controller : MonoBehaviour
    {
        [SerializeField]
        private MazeAgent m_Agent;

        [SerializeField]
        private Maze m_Maze;

        [SerializeField]
        [Tooltip("Width and height of the maze.")]
        private Vector2Int m_MazeSize = new Vector2Int(32, 32);

        private void Awake()
        {
            m_Maze.Buffer = new GridBuffer(Maze.NumChannels, m_MazeSize);
            m_Agent.EpisodeBeginEvent += OnEpisodeBegin;
            m_Agent.FoundFoodEvent += m_Maze.RemoveFood;
        }

        private void OnEpisodeBegin()
        {
            Vector2Int spawnPos = m_Maze.Randomize();
            m_Agent.StartEpisode(m_Maze.Buffer, spawnPos);
        }

        private void OnApplicationQuit()
        {
            m_Agent.EpisodeBeginEvent -= OnEpisodeBegin;
        }
    }
}