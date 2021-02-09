using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using MBaske.Sensors;
using MBaske.MLUtil;

namespace MBaske.Dogfight
{
    public class PilotAgent : Agent, IBulletOwner
    {
        public Vector3 GunPosition { get; private set; }
        public Vector3 GunDirection { get; private set; }

        private Spaceship m_Ship;
        private AsteroidField m_Asteroids;
        private StatsRecorder m_Stats;
        private BulletPool m_Bullets;

        [SerializeField]
        [Tooltip("Reference to front-facing sensor for retrieving detection results.")]
        private SpatialGridSensorComponent m_FrontSensor;
        [SerializeField]
        [Tooltip("Ship-to-ship distance below which agent is considered following opponent.")]
        private float m_FollowDistance = 30;
        [SerializeField]
        [Tooltip("Ship-to-ship forward axis angle below which agent is considered following opponent.")]
        private float m_FollowAngle = 30;
        [SerializeField]
        [Tooltip("Ship-to-ship distance below which target is locked and auto-fire triggered.")]
        private float m_TargetLockDistance = 20;
        [SerializeField]
        [Tooltip("Ship-to-ship forward axis angle below which target is locked and auto-fire triggered.")]
        private float m_TargetLockAngle = 5;
        [SerializeField]
        [Tooltip("Delay between auto-fire shots.")]
        private float m_ReloadTime = 0.2f;
        private float m_ShotTime;
        [SerializeField]
        [Tooltip("Step interval for writing stats to Tensorboard.")]
        private int m_StatsInterval = 120;
        private int m_CollisionCount;
        private int m_HitScoreCount;
        private int m_TargetingCount;

        public override void Initialize()
        {
            m_Asteroids = FindObjectOfType<AsteroidField>();
            m_Bullets = FindObjectOfType<BulletPool>();
            m_Stats = Academy.Instance.StatsRecorder;

            m_Ship = GetComponentInChildren<Spaceship>();
            m_Ship.BulletHitEvent += OnBulletHitSuffered;
            m_Ship.CollisionEvent += OnCollision;
            m_Ship.EnvironmentRadius = m_Asteroids.FieldRadius;
        }

        public override void OnEpisodeBegin()
        {
            m_Asteroids.OnEpisodeBegin();
            m_CollisionCount = 0;
            m_HitScoreCount = 0;
            m_TargetingCount = 0;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(m_Ship.Throttle);
            sensor.AddObservation(m_Ship.Pitch);
            sensor.AddObservation(m_Ship.Roll);
            sensor.AddObservation(m_Ship.NormPosition);
            sensor.AddObservation(m_Ship.NormOrientation);
            sensor.AddObservation(Normalization.Sigmoid(m_Ship.LocalSpin));
            sensor.AddObservation(Normalization.Sigmoid(m_Ship.LocalVelocity));


            Vector3 shipPos = m_Ship.transform.position;
            Vector3 shipFwd = m_Ship.transform.forward;

            if (HasTargetLock(shipPos, shipFwd, out Vector3 targetPos) && CanShoot())
            {
                GunPosition = shipPos + shipFwd;
                GunDirection = (targetPos - shipPos).normalized;

                m_Bullets.Shoot(this);
                m_ShotTime = Time.time;
            }
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            var actions = actionBuffers.DiscreteActions;
            float speed = m_Ship.ManagedUpdate(actions[0] - 1, actions[1] - 1, actions[2] - 1);
            // Reward for forward speed.
            // Increasing this factor will cause the agent to 
            // favour speed over following/targeting opponents.
            AddReward(speed * 0.001f);

            if (StepCount % m_StatsInterval == 0)
            {
                float n = m_StatsInterval;
                m_Stats.Add("Pilot/Speed", speed);
                m_Stats.Add("Pilot/Collision Ratio", m_CollisionCount / n);
                m_Stats.Add("Pilot/Hit Score Ratio", m_HitScoreCount / n);
                m_Stats.Add("Pilot/Target Ratio", m_TargetingCount / n);
                m_CollisionCount = 0;
                m_HitScoreCount = 0;
                m_TargetingCount = 0;
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var actions = actionsOut.DiscreteActions;
            bool shift = Input.GetKey(KeyCode.LeftShift);
            int vert = Mathf.RoundToInt(Input.GetAxis("Vertical"));
            actions[0] = 1 + (shift ? 0 : vert); // throttle
            actions[1] = 1 + (shift ? vert : 0); // pitch
            actions[2] = 1 + Mathf.RoundToInt(Input.GetAxis("Horizontal")); // roll
        }

        private bool HasTargetLock(Vector3 shipPos, Vector3 shipFwd, out Vector3 targetPos)
        {
            targetPos = default;
            bool hasLockAny = false;

            if (m_FrontSensor.HasDetectionResult(out DetectionResult result))
            {
                var list = result.GetDetectionDataList(m_Ship.tag);

                if (list.Count > 0)
                {
                    float maxWeight = 0;
                    var target = list[0];

                    foreach (var item in list)
                    {
                        var pos = ((DetectedCollider)item.AdditionalDetectionData).Position;
                        float weight = GetTargetWeight(shipPos, shipFwd, pos, out bool hasLock);

                        if (weight > maxWeight)
                        {
                            target = item;
                            maxWeight = weight;
                            hasLockAny = hasLockAny || hasLock;
                            targetPos = hasLock ? pos : targetPos;
                        }
                    }

                    if (maxWeight > 0)
                    {
                        // Reward agent for following opponent.
                        AddReward(maxWeight * 2); // TODO factor, prevent zero-sum necessary?
                        // Penalize opponent for being followed.
                        ((DetectedCollider)target.AdditionalDetectionData).Collider.
                            GetComponentInParent<PilotAgent>().AddReward(-maxWeight);

                        m_TargetingCount++;
                    }
                }
            }

            return hasLockAny;
        }

        private float GetTargetWeight(Vector3 shipPos, Vector3 shipFwd, Vector3 targetPos, out bool hasLock)
        {
            Vector3 delta = targetPos - shipPos;
            float angle = Vector3.Angle(shipFwd, delta);

            if (angle <= m_FollowAngle)
            {
                float distance = delta.magnitude;

                if (distance <= m_FollowDistance)
                {
                    hasLock = angle <= m_TargetLockAngle && distance <= m_TargetLockDistance;

                    float rAngle = 1 - angle / m_FollowAngle;
                    float rDistance = 1 - distance / m_FollowDistance;
                    return rAngle * rDistance;
                }
            }

            hasLock = false;
            return 0;
        }

        private bool CanShoot()
        {
            return Time.time - m_ShotTime >= m_ReloadTime;
        }

        private void OnCollision()
        {
            AddReward(-1);
            m_CollisionCount++;
        }

        // Is invoked only if bullet collider is trigger.
        // Otherwise, OnCollision() registers bullet hits.
        public void OnBulletHitSuffered()
        {
            AddReward(-1);
        }

        public void OnBulletHitScored()
        {
            AddReward(1);
            m_HitScoreCount++;
        }

        private void OnDestroy()
        {
            m_Ship.BulletHitEvent -= OnBulletHitSuffered;
            m_Ship.CollisionEvent -= OnCollision;
        }
    }
}