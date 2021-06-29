using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using MBaske.Sensors.Grid;

namespace MBaske.MazeExplorer
{
    /// <summary>
    /// Agent that navigates a <see cref="Maze"/>.
    /// It has to cover as much ground as possible and find food items.
    /// </summary>
    public class MazeAgent : Agent
    {
        /// <summary>
        /// Invoked on <see cref="OnEpisodeBegin"/>.
        /// Triggers <see cref="Maze"/> randomization.
        /// </summary>
        public event Action EpisodeBeginEvent;

        /// <summary>
        /// Invoked on food item found. Will remove item from <see cref="Maze"/>.
        /// </summary>
        public event Action<Vector2Int> FoundFoodEvent;

        [SerializeField]
        [Tooltip("The number of grid cells the agent can observe in any cardinal direction. " +
            "The resulting grid observation will always have odd dimensions, as the agent " +
            "is located at its center position, e.g. radius = 10 results in grid size 21 x 21.")]
        private int m_LookDistance = 10;

        [SerializeField]
        [Tooltip("Amount by which rewards diminish for staying on, or repeat visits to grid " +
            "positions. Initial reward is 0.5 for every move onto a position the agent hasn't" +
            "visited before. Episodes end when rewards drop to -0.5 on any position.")]
        [Range(0, 1)] 
        private float m_RewardDecrement = 0.25f;

        [SerializeField]
        [Tooltip("The animation duration for every agent step at inference.")]
        [Range(0, 0.5f)] 
        private float m_StepDuration = 0.1f;
        private float m_StepTime;

        [SerializeField]
        [Tooltip("Select to enable action masking. Note that a model trained with action " +
            "masking turned on may not behave optimally when action masking is turned off.")]
        private bool m_MaskActions;

        private const int c_Stay = 0; 
        private const int c_Up = 1;
        private const int c_Down = 2;
        private const int c_Left = 3;
        private const int c_Right = 4;

        private GridBuffer m_SensorBuffer;
        private GridBuffer m_MazeBuffer;

        // Current agent position on grid.
        private Vector2Int m_GridPosition;
        private Vector3 m_LocalPosNext;
        private Vector3 m_LocalPosPrev;
        private List<int> m_ValidActions;
        private Vector2Int[] m_Directions;
        
        private bool m_IsTraining;
        // Whether the agent is currently requesting decisions.
        // Agent is inactive during animation at inference.
        private bool m_IsActive;

        /// <inheritdoc/>
        public override void Initialize()
        {
            m_IsTraining = Academy.Instance.IsCommunicatorOn;
            m_ValidActions = new List<int>(5);

            m_Directions = new Vector2Int[]
            {
                Vector2Int.zero,
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            int length = m_LookDistance * 2 + 1;
            // The ColorGridBuffer supports PNG compression.
            m_SensorBuffer = new ColorGridBuffer(Maze.NumChannels, length, length);

            var sensorComp = GetComponent<GridSensorComponent>();
            sensorComp.GridBuffer = m_SensorBuffer;
            // Labels for sensor debugging.
            sensorComp.ChannelLabels = new List<ChannelLabel>()
            {
                new ChannelLabel("Wall", new Color32(0, 128, 255, 255)),
                new ChannelLabel("Food", new Color32(64, 255, 64, 255)),
                new ChannelLabel("Visited", new Color32(255, 64, 64, 255)),
            };
        }

        /// <inheritdoc/>
        public override void OnEpisodeBegin()
        {
            EpisodeBeginEvent.Invoke();
        }

        /// <summary>
        /// Invoked by <see cref="Controller"/> after it randomized the <see cref="Maze"/>.
        /// </summary>
        /// <param name="buffer">The <see cref="Maze"/>'s <see cref="GridBuffer"/></param>
        /// <param name="spawnPos">The agents spawn position on the grid</param>
        public void StartEpisode(GridBuffer buffer, Vector2Int spawnPos)
        {
            m_MazeBuffer ??= buffer;
            m_GridPosition = spawnPos;
            m_LocalPosNext = new Vector3(spawnPos.x, 0, spawnPos.y);
            transform.localPosition = m_LocalPosNext;
        }

        /// <inheritdoc/>
        /// <summary>
        /// Stores valid actions, so that the agent can be penalized for  
        /// invalid ones, in case <see cref="m_MaskActions"/> is set to false.
        /// </summary>
        public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
        {
            m_ValidActions.Clear();
            m_ValidActions.Add(c_Stay);

            for (int action = 1; action < 5; action++)
            {
                bool isValid = m_MazeBuffer.TryRead(Maze.Wall, 
                    m_GridPosition + m_Directions[action],
                    out float value) && value == 0; // no wall

                if (isValid)
                {
                    m_ValidActions.Add(action);
                }
                else if (m_MaskActions)
                {
                    actionMask.SetActionEnabled(0, action, false);
                }
            }
        }

        /// <inheritdoc/>
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            var action = actionBuffers.DiscreteActions[0];
            m_LocalPosPrev = m_LocalPosNext;

            bool isDone;
            if (m_ValidActions.Contains(action))
            {
                m_GridPosition += m_Directions[action];
                m_LocalPosNext = new Vector3(m_GridPosition.x, 0, m_GridPosition.y);

                // Reward/penalize depending on visit value.
                isDone = ValidatePosition(true);
            }
            else
            {
                // Penalize invalid action, m_MaskActions = false.
                AddReward(-1);

                // Don't reward/penalize, but update visit value.
                isDone = ValidatePosition(false);
            }

            if (isDone)
            {
                // Visit value for m_GridPosition reached maximum.
                m_IsActive = false;
                EndEpisode();
            }
            else if (m_IsTraining || action == c_Stay || m_StepDuration == 0)
            {
                // Immediate update.
                transform.localPosition = m_LocalPosNext;
            }
            else
            {
                // Animate to next position.
                m_IsActive = false;
                m_StepTime = 0;
            }
        }

        /// <inheritdoc/>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discreteActionsOut = actionsOut.DiscreteActions;
            discreteActionsOut[0] = c_Stay;

            if (Input.GetKey(KeyCode.D))
            {
                discreteActionsOut[0] = c_Right;
            }
            if (Input.GetKey(KeyCode.W))
            {
                discreteActionsOut[0] = c_Up;
            }
            if (Input.GetKey(KeyCode.A))
            {
                discreteActionsOut[0] = c_Left;
            }
            if (Input.GetKey(KeyCode.S))
            {
                discreteActionsOut[0] = c_Down;
            }
        }

        private void FixedUpdate()
        {
            if (m_IsActive)
            {
                UpdateSensorBuffer();
                RequestDecision();
            }
            else if (m_StepDuration > 0)
            {
                m_StepTime += Time.fixedDeltaTime;
                m_IsActive = m_StepTime >= m_StepDuration;
                // Animate to next position.
                transform.localPosition = Vector3.Lerp(m_LocalPosPrev,
                    m_LocalPosNext, m_StepTime / m_StepDuration);
            }
            else
            {
                // Wait one step before activating.
                m_IsActive = true;
            }
        }

        private bool ValidatePosition(bool rewardAgent)
        {
            // From 0 to +1. 
            float visitValue = m_MazeBuffer.Read(Maze.Visit, m_GridPosition);

            m_MazeBuffer.Write(Maze.Visit, m_GridPosition,
                Mathf.Min(1, visitValue + m_RewardDecrement));

            if (rewardAgent)
            {
                // From +0.5 to -0.5.
                AddReward(0.5f - visitValue);

                if (m_MazeBuffer.Read(Maze.Food, m_GridPosition) == 1)
                {
                    // Reward for finding food.
                    AddReward(1);
                    FoundFoodEvent.Invoke(m_GridPosition);
                }
            }

            return visitValue == 1;
        }

        private void UpdateSensorBuffer()
        {
            m_SensorBuffer.Clear();

            // Current FOV.
            int xMin = m_GridPosition.x - m_LookDistance;
            int xMax = m_GridPosition.x + m_LookDistance;
            int yMin = m_GridPosition.y - m_LookDistance;
            int yMax = m_GridPosition.y + m_LookDistance;

            for (int mx = xMin; mx <= xMax; mx++)
            {
                int sx = mx - xMin;
                for (int my = yMin; my <= yMax; my++)
                {
                    int sy = my - yMin;
                    // TryRead -> FOV might extend beyond maze bounds.
                    if (m_MazeBuffer.TryRead(Maze.Wall, mx, my, out float wall))
                    {
                        // Copy maze -> sensor.
                        m_SensorBuffer.Write(Maze.Wall, sx, sy, wall);
                        m_SensorBuffer.Write(Maze.Food, sx, sy, m_MazeBuffer.Read(Maze.Food, mx, my));
                        m_SensorBuffer.Write(Maze.Visit, sx, sy, m_MazeBuffer.Read(Maze.Visit, mx, my));
                    }
                }
            }
        }
    }
}