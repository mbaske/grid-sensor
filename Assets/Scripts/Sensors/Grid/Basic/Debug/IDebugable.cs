
namespace MBaske.Sensors.Grid
{
    public interface IDebugable
    {
        /// <summary>
        /// Enables/disables debugging.
        /// </summary>
        /// <param name="enabled">Whether debugging is enabled</param>
        /// <param name="target">The <see cref="DebugChannelData"/> instance to use</param>
        void SetDebugEnabled(bool enabled, DebugChannelData target = null);
    }
}