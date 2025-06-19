using System.Runtime.InteropServices;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct EyePositionNormalized
    {
        public long timestamp;
        public Validity left_validity;
        public Vector3D left_xyz;
        public Validity right_validity;
        public Vector3D right_xyz;
    }
}
