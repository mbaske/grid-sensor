using UnityEngine;

namespace MBaske.Sensors.Grid
{
    public class DetectableGameObject2D : DetectableGameObject
    {
        public override Observations InitObservations()
        {
            base.InitObservations();
            if (Observations.IsEmpty)
            {
                // Required.
                Observations.Add(OneHot, "One-Hot");
            }
            return Observations;
        }

        protected override GameObjectShape Shape => m_Shape;

        [SerializeField]
        private GameObjectShape2D m_Shape;

        protected override void ResetShape()
        {
            m_Shape ??= new GameObjectShape2D();
            m_Shape.OnReset();
        }
    }
}
