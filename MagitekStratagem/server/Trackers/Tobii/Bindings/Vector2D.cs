using System.Runtime.InteropServices;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2D
    {
        public float x;
        public float y;

        public static implicit operator System.Numerics.Vector2(Vector2D v)
        {
            return new System.Numerics.Vector2(v.x, v.y);
        }
    }
}
