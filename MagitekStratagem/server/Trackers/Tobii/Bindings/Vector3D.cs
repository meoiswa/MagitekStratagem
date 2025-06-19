using System.Runtime.InteropServices;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3D
    {
        public float x;
        public float y;
        public float z;

        public static implicit operator System.Numerics.Vector3(Vector3D v)
        {
            return new System.Numerics.Vector3(v.x, v.y, v.z);
        }
    }
}
