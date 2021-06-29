using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Helper component for creating <see cref="GridEditor3D"/>.
    /// This is needed because NaughtyAttributes don't work with custom editors.
    /// </summary>
    public class GridEditorHelper2D : MonoBehaviour
    {
        /// <summary>
        /// Returns the <see cref="GridSensorComponent2D"/> to <see cref="GridEditor3D"/>.
        /// </summary>
        /// <returns><see cref="GridSensorComponent2D"/></returns>
        public GridSensorComponent2D GetSensorComponent()
        {
            if (TryGetComponent(out GridSensorComponent2D comp))
            {
                comp.OnEditorInit();
                return comp;
            }

            return null;
        }
    }
}