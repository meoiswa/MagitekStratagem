namespace MagitekStratagemServer.Trackers.Eyeware.Bindings.Client
{
    public struct AffineTransform3D
    {
        public float[,] Rotation; // 3x3 matrix
        public Vector3D Translation;

        public Vector3D ToEulerAngles()
        {
            // Map rotation matrix to Euler angles (Pitch, Yaw, Roll)
            var m = Rotation; // float[3,3] rotation matrix
                              // Extract Euler angles (Pitch=X, Yaw=Y, Roll=Z) from rotation matrix
            float sy = (float)Math.Sqrt(m[2, 1] * m[2, 1] + m[2, 2] * m[2, 2]);
            bool singular = sy < 1e-6;
            float x, y, z;
            if (!singular)
            {
                x = (float)Math.Atan2(m[2, 1], m[2, 2]); // Pitch
                y = (float)Math.Atan2(-m[2, 0], sy);     // Yaw
                z = (float)Math.Atan2(m[1, 0], m[0, 0]);  // Roll
            }
            else
            {
                x = (float)Math.Atan2(-m[1, 2], m[1, 1]);
                y = (float)Math.Atan2(-m[2, 0], sy);
                z = 0;
            }

            return new Vector3D
            {
                X = x,
                Y = y,
                Z = z
            };
        }
    }
}
