using UnityEngine;

namespace MBaske.Sensors.Grid
{
    public class DetectableGameObject3D : DetectableGameObject
    {
        [SerializeField, Tooltip("Enable to add optional one-hot observation." +
            "\nDistance is being observed regardless.")]
        private bool m_AddOneHotObservation;

        public override Observations InitObservations()
        {
            base.InitObservations();
            if (m_AddOneHotObservation)
            {
                // Optional.
                Observations.Add(OneHot, "One-Hot");
            }
            return Observations;
        }

        protected override GameObjectShape Shape => m_Shape;

        [SerializeField]
        private GameObjectShape3D m_Shape;

        protected override void ResetShape()
        {
            m_Shape ??= new GameObjectShape3D();
            m_Shape.OnReset();
        }
    }
}
