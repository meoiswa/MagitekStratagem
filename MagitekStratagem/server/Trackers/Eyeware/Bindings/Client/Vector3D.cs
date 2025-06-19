namespace MagitekStratagemServer.Trackers.Eyeware.Bindings.Client
{
    public struct Vector3D
    {
        public float X;
        public float Y;
        public float Z;

        public static implicit operator System.Numerics.Vector3(Vector3D v)
        {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }
    }
}
