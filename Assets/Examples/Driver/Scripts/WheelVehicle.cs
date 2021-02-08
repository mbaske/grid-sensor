using UnityEngine;
using System;

namespace VehicleBehaviour 
{
    [RequireComponent(typeof(Rigidbody))]
    public class WheelVehicle : MonoBehaviour 
    {
        public event Action CollisionEvent;

        /// <summary>
        /// Normalized throttle control value. 
        /// </summary>
        public float Throttle { get; private set; }
        /// <summary>
        /// Normalized steering control value. 
        /// </summary>
        public float Steering { get; private set; }

        public Vector3 LocalVelocity => transform.InverseTransformVector(m_Rigidbody.velocity);
        public Vector3 LocalSpin => transform.InverseTransformVector(m_Rigidbody.angularVelocity);
        public Vector3 Gyro => new Vector3(transform.right.y, transform.up.y, transform.forward.y);

        public bool IsMoving => m_Rigidbody.velocity.sqrMagnitude > 1;

        [SerializeField]
        private float m_ControlIncrement = 0.05f;
        [SerializeField]
        private float m_ControlAttenuate = 0.95f;

        /*
         * This code is part of Arcade Car Physics for Unity by Saarg (2018)
         * 
         * This is distributed under the MIT Licence (see LICENSE.md for details)
         */

        /* 
         *  Turn input curve: x real input, y value used
         *  My advice (-1, -1) tangent x, (0, 0) tangent 0 and (1, 1) tangent x
         */
        [SerializeField] AnimationCurve turnInputCurve = AnimationCurve.Linear(-1.0f, -1.0f, 1.0f, 1.0f);
        [SerializeField] WheelCollider[] driveWheel;
        [SerializeField] WheelCollider[] turnWheel;
        /*
         *  Motor torque represent the torque sent to the wheels by the motor with x: speed in km/h and y: torque
         *  The curve should start at x=0 and y>0 and should end with x>topspeed and y<0
         *  The higher the torque the faster it accelerate
         *  the longer the curve the faster it gets
         */
        [SerializeField] AnimationCurve motorTorque = new AnimationCurve(new Keyframe(0, 200), new Keyframe(50, 300), new Keyframe(200, 0));

        // Differential gearing ratio
        [Range(2, 16)]
        [SerializeField] float diffGearing = 4.0f;
        // Basicaly how hard it brakes
        [SerializeField] float brakeForce = 1500.0f;
        // Max steering angle, usualy higher for drift car
        [Range(0f, 50.0f)]
        [SerializeField] float steerAngle = 30.0f;
        // The value used in the steering Lerp, 1 is instant (Strong power steering), and 0 is not turning at all
        [Range(0.001f, 1.0f)]
        [SerializeField] float steerSpeed = 0.2f;

        [SerializeField] float maxSpeed = 100;
        /*
         *  The center of mass is set at the start and changes the car behavior A LOT
         *  I recomment having it between the center of the wheels and the bottom of the car's body
         *  Move it a bit to the from or bottom according to where the engine is
         */
        [SerializeField] Transform centerOfMass;
        // Force aplied downwards on the car, proportional to the car speed
        [Range(0.5f, 10f)]
        [SerializeField] float downforce = 1.0f;

        WheelCollider[] m_WheelColliders;
        Rigidbody m_Rigidbody;
        
        public void Initialize() 
        {
            m_WheelColliders = GetComponentsInChildren<WheelCollider>();
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.centerOfMass = centerOfMass ? centerOfMass.localPosition : m_Rigidbody.centerOfMass;

            // Set the motor torque to a non null value because 0 means the wheels won't turn no matter what
            foreach (WheelCollider wheel in m_WheelColliders)
            {
                wheel.motorTorque = 0.0001f;
            }
        }

        public void ManagedReset(Vector3 pos, Quaternion rot)
        {
            transform.position = pos;
            transform.rotation = rot;
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
            m_Rigidbody.Sleep();
            Throttle = 0;
            Steering = 0;
        }

        public float ManagedUpdate(int throttle, int steering)
        {
            Throttle = throttle == 0
                ? Throttle * m_ControlAttenuate
                : Mathf.Clamp(Throttle + throttle * m_ControlIncrement, -1, 1);

            Steering = steering == 0
                ? Steering * m_ControlAttenuate
                : Mathf.Clamp(Steering + steering * m_ControlIncrement, -1, 1);

            float z = LocalVelocity.z;
            UpdatePhysics(z * 3.6f); // factor taken from orig. code

            return z;
        }

        public bool IsGrounded()
        {
            return Physics.Raycast(transform.position + Vector3.up, Vector3.down, 10, 1);
        }

        private void UpdatePhysics(float speed) 
        {
            float angle = turnInputCurve.Evaluate(Steering) * steerAngle;
            foreach (WheelCollider wheel in turnWheel)
            {
                wheel.steerAngle = Mathf.Lerp(wheel.steerAngle, angle, steerSpeed);
            }

            foreach (WheelCollider wheel in m_WheelColliders)
            {
                wheel.brakeTorque = 0;
            }

            if (speed < maxSpeed && Mathf.Sign(speed) == Mathf.Sign(Throttle))
            {
                foreach (WheelCollider wheel in driveWheel)
                {
                    wheel.motorTorque = Throttle * motorTorque.Evaluate(speed) * diffGearing / driveWheel.Length;
                }
            }
            else
            {
                foreach (WheelCollider wheel in m_WheelColliders)
                {
                    wheel.brakeTorque = Mathf.Abs(Throttle) * brakeForce;
                }
            }

            m_Rigidbody.AddForce(-transform.up * speed * downforce);
        }

        private void OnCollisionEnter(Collision collision)
        {
            CollisionEvent?.Invoke();
        }

        private void OnCollisionStay(Collision collision)
        {
            CollisionEvent?.Invoke();
        }
    }
}
