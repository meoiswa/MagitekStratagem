namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    public static partial class StreamEngine
    {
        public enum Error
        {
            NO_ERROR,
            INTERNAL,
            INSUFFICIENT_LICENSE,
            NOT_SUPPORTED,
            NOT_AVAILABLE,
            FAILED,
            TIMED_OUT,
            ALLOCATION_FAILED,
            INVALID_PARAMETER,
            CALIBRATION_ALREADY_STARTED,
            CALIBRATION_NOT_STARTED,
            ALREADY_SUBSCRIBED,
            NOT_SUBSCRIBED,
            OPERATION_FAILED,
            CONFLICTING_API_INSTANCES,
            CALIBRATION_BUSY,
            CALLBACK_IN_PROGRESS,
            TOO_MANY_SUBSCRIBERS,
            CONNECTION_FAILED_DRIVER,
            UNAUTHORIZED
        }
    }
}
