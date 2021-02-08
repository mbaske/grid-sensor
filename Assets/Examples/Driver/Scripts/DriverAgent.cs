using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using VehicleBehaviour;
using MBaske.MLUtil;

namespace MBaske.Driver
{
    public class DriverAgent : Agent
    {
        private Road m_Road;
        private WheelVehicle m_Car;
        private Vector3 m_ChunkPos;
        private StatsRecorder m_Stats;

        [SerializeField]
        [Tooltip("Timeout in seconds after which episode ends if agent doesn't move.")]
        private float m_Timeout = 5;
        private float m_Time;
        [SerializeField]
        [Tooltip("Step interval for writing stats to Tensorboard.")]
        private int m_StatsInterval = 120;
        private int m_CollisionCount;

        private const int c_CheckStateInterval = 12;
        

        public override void Initialize()
        {
            m_Stats = Academy.Instance.StatsRecorder;
            m_Road = GetComponentInChildren<Road>();
            m_Road.Initialize();

            m_Car = GetComponentInChildren<WheelVehicle>();
            m_Car.CollisionEvent += OnCollision;
            m_Car.Initialize();
        }

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

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(m_Car.Throttle);
            sensor.AddObservation(m_Car.Steering);
            sensor.AddObservation(m_Car.Gyro);
            sensor.AddObservation(Normalization.Sigmoid(m_Car.LocalSpin));
            sensor.AddObservation(Normalization.Sigmoid(m_Car.LocalVelocity));
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            var actions = actionBuffers.DiscreteActions;
            float speed = m_Car.ManagedUpdate(actions[0] - 1, actions[1] - 1);
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

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var actions = actionsOut.DiscreteActions;
            actions[0] = 1 + Mathf.RoundToInt(Input.GetAxis("Vertical"));
            actions[1] = 1 + Mathf.RoundToInt(Input.GetAxis("Horizontal"));
        }

        private void CheckState()
        {
            if (!m_Car.IsGrounded() || Time.time - m_Time > m_Timeout)
            {
                EndEpisode();
            }
            else if (m_Car.IsMoving)
            {
                m_Time = Time.time;

                // TBD chunk length = 12m, replace at 30m distance.
                if ((m_Car.transform.position - m_ChunkPos).sqrMagnitude > 900)
                {
                    m_Road.ReplaceFirstChunk();
                    m_ChunkPos = m_Road.FirstChunkTF.position;
                }
            }
        }

        private void OnCollision()
        {
            AddReward(-1);
            m_CollisionCount++;
        }

        private void OnDestroy()
        {
            m_Car.CollisionEvent -= OnCollision;
        }
    }
}