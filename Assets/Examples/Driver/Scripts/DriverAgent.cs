using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityStandardAssets.Vehicles.Car;
using MBaske.MLUtil;

namespace MBaske.Driver
{
    /// <summary>
    /// Agent that controls a car and has to avoid obstacles.
    /// </summary>
    public class DriverAgent : Agent
    {
        private Road m_Road;
        private CarController m_Car;
        private Vector3 m_ChunkPos;
        private StatsRecorder m_Stats;

        [SerializeField]
        [Tooltip("Timeout in seconds after which episode ends if agent doesn't move. " +
            "Set to 0 for disabling timeout.")]
        [Min(0)] private float m_Timeout = 5;
        private float m_Time;
        [SerializeField]
        [Tooltip("Step interval for writing stats to Tensorboard.")]
        [Min(50)] private int m_StatsInterval = 120;
        private int m_CollisionCount;

        private const int c_CheckStateInterval = 20;

        /// <inheritdoc/>
        public override void Initialize()
        {
            m_Stats = Academy.Instance.StatsRecorder;
            m_Road = GetComponentInChildren<Road>();
            m_Road.Initialize();

            m_Car = GetComponentInChildren<CarController>();
            m_Car.CollisionEvent += OnCollision;
            m_Car.Initialize();
        }

        /// <inheritdoc/>
        public override void OnEpisodeBegin()
        {
            m_Road.ManagedReset();
            var tf = m_Road.FirstChunkTF;
            // Start offset.
            var carPos = tf.position 
                + Vector3.up * 2 
                + tf.forward * 5;
            m_Car.ManagedReset(carPos, tf.rotation);
            m_ChunkPos = tf.position;
            m_CollisionCount = 0;
            m_Time = Time.time;
        }

        /// <inheritdoc/>
        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(m_Car.NormSteer);
            sensor.AddObservation(Normalization.Sigmoid(m_Car.LocalSpin));
            sensor.AddObservation(Normalization.Sigmoid(m_Car.LocalVelocity));
        }

        /// <inheritdoc/>
        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            var actions = actionBuffers.ContinuousActions;
            m_Car.Move(actions[0], actions[1], actions[1], actions[2]);
            float speed = m_Car.ForwardSpeed;
            AddReward(speed * 0.01f);

            if (StepCount % m_StatsInterval == 0)
            {
                m_Stats.Add("Driver/Speed", speed);
                m_Stats.Add("Driver/Collision Ratio", m_CollisionCount / (float)m_StatsInterval);
                m_CollisionCount = 0;
            }

            if (StepCount % c_CheckStateInterval == 0)
            {
                CheckState();
            }
        }

        /// <inheritdoc/>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var actions = actionsOut.ContinuousActions;
            actions[0] = Mathf.RoundToInt(Input.GetAxis("Horizontal"));
            actions[1] = Mathf.RoundToInt(Input.GetAxis("Vertical"));
            actions[2] = Mathf.RoundToInt(Input.GetAxis("Jump"));
        }

        private void CheckState()
        {
            if (!m_Car.IsGrounded() || (m_Timeout > 0 && Time.time - m_Time > m_Timeout))
            {
                // Agent veered off the road or
                // stayed on the same chunk for too long.
                EndEpisode();
            }
            else if (m_Car.IsMoving())
            {
                m_Time = Time.time;

                // TBD chunk length = 16m, replace at 32m distance.
                if ((m_Car.transform.position - m_ChunkPos).sqrMagnitude > 1024)
                {
                    m_Road.ReplaceFirstChunk();
                    m_ChunkPos = m_Road.FirstChunkTF.position;
                }
            }
        }

        private void OnCollision()
        {
            AddReward(-0.1f);
            m_CollisionCount++;
        }

        private void OnApplicationQuit()
        {
            m_Car.CollisionEvent -= OnCollision;
        }
    }
}