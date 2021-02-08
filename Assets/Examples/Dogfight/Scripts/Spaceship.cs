using UnityEngine;
using System;

namespace MBaske.Dogfight
{
    public class Spaceship : MonoBehaviour
    {
        public event Action CollisionEvent;
        public event Action BulletHitEvent;

        /// <summary>
        /// Normalized position on outward pointing normal,
        /// between center and brake threshold radius.
        /// </summary>
        public float NormPosition { get; private set; }
        /// <summary>
        /// Normalized forward angle relative to outward pointing normal.
        /// </summary>
        public float NormOrientation { get; private set; }

        /// <summary>
        /// Normalized throttle control value. 
        /// </summary>
        public float Throttle { get; private set; }
        /// <summary>
        /// Normalized pitch control value. 
        /// </summary>
        public float Pitch { get; private set; }
        /// <summary>
        /// Normalized roll control value. 
        /// </summary>
        public float Roll { get; private set; }

        public Vector3 LocalVelocity => transform.InverseTransformVector(m_Rigidbody.velocity);
        public Vector3 LocalSpin => transform.InverseTransformVector(m_Rigidbody.angularVelocity);

        public Vector3 LocalPosition
        {
            get { return transform.localPosition; }
            set
            {
                transform.localPosition = value;
                transform.rotation = Quaternion.LookRotation(-value);
                Stop();
            }
        }

        public float EnvironmentRadius
        {
            set
            {
                m_brakeZoneLength = value * 0.1f;
                m_BrakeThreshRadius = value * 0.8f;
                m_BrakeThreshRadiusSqr = m_BrakeThreshRadius * m_BrakeThreshRadius;
            }
        }
        private float m_brakeZoneLength;
        private float m_BrakeThreshRadius;
        private float m_BrakeThreshRadiusSqr;

        [SerializeField]
        private float m_ControlIncrement = 0.04f;
        [SerializeField]
        private float m_ControlAttenuate = 0.95f;
        [SerializeField]
        private float m_BackwardSpeedFactor = 0.25f;

        private Rigidbody m_Rigidbody;


        public float ManagedUpdate(int throttle, int pitch, int roll)
        {
            Vector3 pos = LocalPosition;
            Vector3 fwd = transform.forward;
            Vector3 velocity = m_Rigidbody.velocity;
            Vector3 normal = pos.normalized;
            float sqrMag = pos.sqrMagnitude;

            // Update control values.

            Throttle = throttle == 0
                ? Throttle * m_ControlAttenuate
                : Mathf.Clamp(Throttle + throttle * m_ControlIncrement, -1, 1);

            velocity += fwd * Throttle;
            

            Pitch = pitch == 0
                ? Pitch * m_ControlAttenuate
                : Mathf.Clamp(Pitch + pitch * m_ControlIncrement, -1, 1);

            Roll = roll == 0
                ? Roll * m_ControlAttenuate
                : Mathf.Clamp(Roll + roll * m_ControlIncrement, -1, 1);

            m_Rigidbody.AddTorque(transform.right * Pitch + fwd * -Roll, ForceMode.VelocityChange);


            // Update observations.

            NormPosition = Mathf.Clamp01(sqrMag / m_BrakeThreshRadiusSqr) * 2 - 1;
            NormOrientation = Vector3.Angle(normal, fwd) / 90f - 1;


            // Counter outward velocity when ship enters brake zone. 

            if (sqrMag > m_BrakeThreshRadiusSqr && NormOrientation < 0)
            {
                float outwardVelocity = Vector3.Dot(normal, velocity);
                float brakeStrength = (pos.magnitude - m_BrakeThreshRadius) / m_brakeZoneLength;
                velocity -= normal * outwardVelocity * brakeStrength;
            }

            float fwdSpeed = Vector3.Dot(fwd, velocity);
            if (fwdSpeed < 0)
            {
                velocity *= m_BackwardSpeedFactor;
            }

            m_Rigidbody.velocity = velocity;

            return fwdSpeed;
        }


        private void Stop()
        {
            m_Rigidbody ??= GetComponent<Rigidbody>();
            m_Rigidbody.angularVelocity = Vector3.zero;
            m_Rigidbody.velocity = Vector3.zero;

            Throttle = 0;
            Pitch = 0;
            Roll = 0;
        }

        private void Awake()
        {
            Stop();
        }

        private void OnCollisionEnter(Collision collision)
        {
            CollisionEvent?.Invoke();
        }

        private void OnCollisionStay(Collision collision)
        {
            CollisionEvent?.Invoke();
        }

        private void OnTriggerEnter(Collider other)
        {
            BulletHitEvent?.Invoke();
        }
    }
}