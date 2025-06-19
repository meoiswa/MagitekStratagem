using System.Runtime.InteropServices;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HeadPose
    {
        public long timestamp;
        public Validity position_validity;
        public Vector3D position_xyz;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public Validity[] rotation_validity_xyz;
        public Vector3D rotation_xyz;
    }
}
