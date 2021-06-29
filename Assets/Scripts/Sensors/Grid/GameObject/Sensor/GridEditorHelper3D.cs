using UnityEngine;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Helper component for creating a <see cref="GridEditor3D"/>.
    /// This is needed because NaughtyAttributes don't work with custom editors.
    /// </summary>
    public class GridEditorHelper3D : MonoBehaviour
    {
        /// <summary>
        /// Returns the <see cref="GridSensorComponent3D"/> to <see cref="GridEditor3D"/>.
        /// </summary>
        /// <returns><see cref="GridSensorComponent3D"/></returns>
        public GridSensorComponent3D GetSensorComponent()
        {
            if (TryGetComponent(out GridSensorComponent3D comp))
            {
                comp.OnEditorInit();
                return comp;
            }

            return null;
        }
    }
}