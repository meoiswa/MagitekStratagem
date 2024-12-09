namespace MagitekStratagemServer.Trackers.Eyeware.Bindings
{
    public enum TrackingConfidence
    {
        Unreliable = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public struct ScreenGazeInfo
    {
        public uint ScreenId;
        public uint X;
        public uint Y;
        public TrackingConfidence Confidence;
        public bool IsLost;
    }

    public struct GazeInfo
    {
        public Ray3D LeftEyeRay;
        public Ray3D RightEyeRay;
        public TrackingConfidence ConfidenceLeft;
        public TrackingConfidence ConfidenceRight;
        public bool IsLost;
    }

    public struct BlinkInfo
    {
        public bool LeftEyeClosed;
        public bool RightEyeClosed;
        public TrackingConfidence ConfidenceLeft;
        public TrackingConfidence ConfidenceRight;
        public bool IsLost;
    }

    public struct HeadPoseInfo
    {
        public AffineTransform3D Transform;
        public bool IsLost;
        public ulong TrackSessionUid;
    }

    public struct TrackedPersonInfo
    {
        public HeadPoseInfo HeadPose;
        public GazeInfo Gaze;
        public ScreenGazeInfo ScreenGaze;
        public BlinkInfo Blink;
    }

    public struct TrackedUser
    {
        public uint Id;
        public TrackedPersonInfo TrackingInfo;

        public TrackedUser(uint id)
        {
            Id = id;
            TrackingInfo = new TrackedPersonInfo();
        }
    }

    public struct TrackingEvent
    {
        public List<TrackedUser> People;
        public double Timestamp;
    }

    public struct Vector2D
    {
        public float X;
        public float Y;
    }

    public struct Vector3D
    {
        public float X;
        public float Y;
        public float Z;
    }

    public struct Ray3D
    {
        public Vector3D Origin;
        public Vector3D Direction;
    }

    public struct AffineTransform3D
    {
        public float[,] Rotation; // 3x3 matrix
        public Vector3D Translation;
    }
    public enum NetworkError
    {
        Timeout = 0,
        UnknownError = 1
    }

    public class NetworkException : Exception
    {
        public NetworkException(string errorMsg) : base(errorMsg) { }
    }
}
