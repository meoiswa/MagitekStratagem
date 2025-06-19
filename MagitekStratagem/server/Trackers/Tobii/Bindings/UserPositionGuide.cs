using System.Runtime.InteropServices;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UserPositionGuide
    {
        public long timestamp;
        public Validity left_position_validity;
        public Vector3D left_position_normalized_xyz;
        public Validity right_position_validity;
        public Vector3D right_position_normalized_xyz;
    }
}
