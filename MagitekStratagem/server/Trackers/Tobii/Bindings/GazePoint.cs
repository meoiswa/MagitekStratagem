using System.Runtime.InteropServices;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GazePoint
    {
        public long timestamp;
        public Validity validity;
        public Vector2D position;
    }
}
