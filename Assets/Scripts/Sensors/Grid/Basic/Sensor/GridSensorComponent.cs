using Unity.MLAgents.Sensors;

namespace MBaske.Sensors.Grid
{
    /// <summary>
    /// Minimal concrete implementation of a Grid Sensor Component.
    /// 
    /// Write observations via the <see cref="GridSensorComponentBase.GridBuffer">
    /// property: m_GridSensorComponent.GridBuffer.Write(channel, x, y, value)
    /// </summary>
    public class GridSensorComponent : GridSensorComponentBase
    {
        /// <inheritdoc/>
        public override ISensor[] CreateSensors()
        {
            if (GridBuffer == null)
            {
                // Create GridBuffer if none was provided.
                // The ColorGridBuffer supports PNG compression.
                GridShape.Validate();
                GridBuffer = new ColorGridBuffer(GridShape);
            }

            return base.CreateSensors();
        }
    }
}