/*
 * This code is part of Arcade Car Physics for Unity by Saarg (2018)
 * 
 * This is distributed under the MIT Licence (see LICENSE.md for details)
 */
using UnityEngine;

namespace VehicleBehaviour 
{
    [RequireComponent(typeof(WheelCollider))]

    /*
        Okay so This scripts keeps the Wheel model aligned with the wheel collider component
        It is not perfect and sometimes depending on the model you're using or if it rains outside
        you might need to add localRotOffset euler rotation to have your wheels in place
        Just hit play and check if your wheels are the way you want and adjust localRotOffset if needed.
     */

    public class Suspension : MonoBehaviour 
    {
        public GameObject wheelModel;
        private WheelCollider _wheelCollider;
        public Vector3 localRotOffset;

        void Start()
        {
            _wheelCollider = GetComponent<WheelCollider>();
        }

        void Update()
        {
            _wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            wheelModel.transform.rotation = rot;
            //wheelModel.transform.localRotation *= Quaternion.Euler(localRotOffset);
            wheelModel.transform.position = pos;
            //_wheelCollider.GetGroundHit(out WheelHit wheelHit);
        }
    }
}
