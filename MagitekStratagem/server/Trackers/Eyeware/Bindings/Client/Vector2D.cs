namespace MagitekStratagemServer.Trackers.Eyeware.Bindings.Client
{
    public struct Vector2D
    {
        public float X;
        public float Y;

        public static implicit operator System.Numerics.Vector2(Vector2D v)
        {
            return new System.Numerics.Vector2(v.X, v.Y);
        }
    }
}
