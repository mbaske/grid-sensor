using UnityEngine;

namespace MBaske.Driver
{
    public class DetachedSensors : MonoBehaviour
    {
        [SerializeField]
        private Transform m_ReferenceFrame;
        [SerializeField]
        private Vector3 m_Offset = Vector3.up;

        private void Update()
        {
            transform.position = m_ReferenceFrame.position + m_Offset;
            transform.rotation = m_ReferenceFrame.rotation;
        }
    }
}