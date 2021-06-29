using UnityEngine;
using System.Linq;

namespace MBaske.Dogfight
{
	/// <summary>
	/// Rotates the camera around the closest <see cref="Spaceship"/>.
	/// </summary>
	public class RotatingCamera : MonoBehaviour
	{
		[SerializeField]
		private float m_Distance = 20;
		[SerializeField]
		private float m_Speed = 10;
		private float m_Angle;
		[SerializeField]
		private float m_SmoothTime = 1;
		[SerializeField]
		private int m_UpdateInterval = 60;
		private int m_StepCount;

		private Transform[] m_Targets;
		private Transform m_Target;
		private Vector3 m_Position;
		private Vector3 m_Velocity;

		private void Start()
		{
			m_Targets = FindObjectsOfType<Spaceship>().Select(x => x.transform).ToArray();
			enabled = m_Targets.Length > 0;
			m_Target = enabled ? m_Targets[0] : null;
		}

		private void LateUpdate()
		{
			if (++m_StepCount % m_UpdateInterval == 1)
			{
				Vector3 pos = transform.position;
				float min = (m_Target.position - pos).sqrMagnitude;
				
				for (int i = 0; i < m_Targets.Length; i++)
				{
					float d = (m_Targets[i].position - pos).sqrMagnitude;
					if (d < min)
					{
						m_Target = m_Targets[i];
						min = d;
					}
				}
			}

			m_Position = Vector3.SmoothDamp(
				m_Position,
				m_Target.position, 
				ref m_Velocity, 
				m_SmoothTime);

			m_Angle += m_Speed * Time.deltaTime;

			transform.position = m_Position
				+ Quaternion.AngleAxis(m_Angle, Vector3.up)
				* Vector3.forward * m_Distance;

			transform.LookAt(m_Position);
		}
	}
}