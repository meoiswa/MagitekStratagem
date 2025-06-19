using System.Runtime.InteropServices;
using System.Text;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    public static partial class StreamEngine
    {
        private static Action<string> Log = (message) => { Console.WriteLine("StreamEngine: " + message); };

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void url_receiver(string url, nint user_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GazePointCallback(ref GazePoint gaze_point, nint user_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GazeOriginCallback(ref GazeOrigin gaze_origin, nint user_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void EyePositionNormalizedCallback(ref EyePositionNormalized eye_position, nint user_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void UserPresenceCallback(UserPresenceStatus status, long timestamp_us, nint user_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void HeadPoseCallback(ref HeadPose head_pose, nint user_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NotificationsCallback(ref Notification notification, nint user_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void UserPositionGuideCallback(ref UserPositionGuide user_position_guide, nint user_data);

        public const string Library = "tobii_stream_engine.dll";

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_get_api_version")]
        private static extern Error get_api_version(out TobiiVersion version);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_api_create")]
        private static extern Error api_create(out nint api, nint custom_alloc, nint custom_log);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_api_destroy")]
        private static extern Error api_destroy(nint api);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl,
                    EntryPoint = "tobii_enumerate_local_device_urls")]
        private static extern Error enumerate_local_device_urls(nint api, url_receiver receiver, nint userData);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "tobii_device_create")]
        private static extern Error device_create(nint api, string url, FieldOfUse fieldOfUse, out nint device);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl,
                    EntryPoint = "tobii_device_destroy")]
        private static extern Error device_destroy(nint device);


        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_gaze_point_subscribe")]
        private static extern Error gaze_point_subscribe(nint device, GazePointCallback callback, nint user_data);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_gaze_point_unsubscribe")]
        private static extern Error gaze_point_unsubscribe(nint device);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_gaze_origin_subscribe")]
        private static extern Error gaze_origin_subscribe(nint device, GazeOriginCallback callback, nint user_data);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_gaze_origin_unsubscribe")]
        private static extern Error gaze_origin_unsubscribe(nint device);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_eye_position_normalized_subscribe")]
        private static extern Error eye_position_normalized_subscribe(nint device, EyePositionNormalizedCallback callback, nint user_data);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_eye_position_normalized_unsubscribe")]
        private static extern Error eye_position_normalized_unsubscribe(nint device);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_user_presence_subscribe")]
        private static extern Error user_presence_subscribe(nint device, UserPresenceCallback callback, nint user_data);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_user_presence_unsubscribe")]
        private static extern Error user_presence_unsubscribe(nint device);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_head_pose_subscribe")]
        private static extern Error head_pose_subscribe(nint device, HeadPoseCallback callback, nint user_data);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_head_pose_unsubscribe")]
        private static extern Error head_pose_unsubscribe(nint device);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_notifications_subscribe")]
        private static extern Error notifications_subscribe(nint device, NotificationsCallback callback, nint user_data);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_notifications_unsubscribe")]
        private static extern Error notifications_unsubscribe(nint device);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_user_position_guide_subscribe")]
        private static extern Error user_position_guide_subscribe(nint device, UserPositionGuideCallback callback, nint user_data);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_user_position_guide_unsubscribe")]
        private static extern Error user_position_guide_unsubscribe(nint device);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl,
                    EntryPoint = "tobii_wait_for_callbacks")]
        private static extern Error wait_for_callbacks(int device_count, nint[] devices);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl,
                    EntryPoint = "tobii_device_process_callbacks")]
        private static extern Error device_process_callbacks(nint device);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "tobii_get_device_name")]
        private static extern Error get_device_name(nint device, StringBuilder device_name);

        public static TobiiVersion GetApiVersion()
        {
            TobiiVersion version;
            var error = get_api_version(out version);
            if (error != Error.NO_ERROR)
            {
                throw new TobiiException("Failed to get API version: " + error);
            }
            return version;
        }

        public static Api CreateApi()
        {
            nint ptr;
            var error = api_create(out ptr, nint.Zero, nint.Zero);
            if (error != Error.NO_ERROR)
            {
                throw new TobiiException("Failed to create API: " + error);
            }
            return new Api(ptr);
        }

        public static void DestroyApi(nint api)
        {
            Log("Disposing of Api " + api);
            var error = api_destroy(api);
            if (error != Error.NO_ERROR)
            {
                throw new TobiiException("Failed to destroy API: " + error);
            }
        }

        public static List<string> EnumerateLocalDevices(nint api)
        {
            var urls = new List<string>();
            url_receiver receiver = (url, userData) => urls.Add(url);
            var error = enumerate_local_device_urls(api, receiver, nint.Zero);
            if (error != Error.NO_ERROR)
            {
                throw new TobiiException("Failed to enumerate local devices: " + error);
            }
            return urls;
        }

        public static Device CreateDevice(nint api, string url)
        {
            nint ptr;
            var error = device_create(api, url, FieldOfUse.INTERACTIVE, out ptr);
            if (error != Error.NO_ERROR)
            {
                throw new TobiiException("Failed to create device: " + error);
            }
            return new Device(ptr);
        }

        public static void DestroyDevice(nint device)
        {
            Log("Disposing of Device " + device);
            var error = device_destroy(device);
            if (error != Error.NO_ERROR)
            {
                throw new TobiiException("Failed to destroy device: " + error);
            }
        }

        public static void GazePointSubscribe(nint device, GazePointCallback callback)
        {
            var error = gaze_point_subscribe(device, callback, nint.Zero);
            if (error != Error.NO_ERROR)
            {
                throw new TobiiException("Failed to subscribe to gaze point: " + error);
            }
        }

        public static void GazePointUnsubscribe(nint device)
        {
            Log("Unsusbcribing from Gaze Point " + device);
            var error = gaze_point_unsubscribe(device);
            if (error != Error.NO_ERROR)
            {
                throw new TobiiException("Failed to unsubscribe from gaze point: " + error);
            }
        }

        public static void HeadposeSubscribe(nint device, HeadPoseCallback callback)
        {
            var error = head_pose_subscribe(device, callback, nint.Zero);
            if (error != Error.NO_ERROR)
            {
                throw new TobiiException("Failed to subscribe to head pose: " + error);
            }
        }

        public static void HeadposeUnsubscribe(nint device)
        {
            Log("Unsusbcribing from Head Pose " + device);
            var error = head_pose_unsubscribe(device);
            if (error != Error.NO_ERROR)
            {
                throw new TobiiException("Failed to unsubscribe from head pose: " + error);
            }
        }

        public static bool WaitForCallbacks(IEnumerable<nint> devices)
        {
            if (devices != null && devices.Any())
            {
                var devicePtrs = devices.ToArray();
                var error = wait_for_callbacks(devicePtrs.Length, devicePtrs);
                if (error != Error.NO_ERROR && error != Error.TIMED_OUT)
                {
                    throw new TobiiException("Failed to wait for callbacks: " + error);
                }

                return error == Error.NO_ERROR;
            }

            return false;
        }

        public static void ProcessCallbacks(nint device)
        {
            if (device != nint.Zero)
            {
                var error = device_process_callbacks(device);
                if (error != Error.NO_ERROR)
                {
                    throw new TobiiException("Failed to process callbacks: " + error);
                }
            }
        }

        public static string GetDeviceName(nint device)
        {
            var deviceName = new StringBuilder(256);
            var error = get_device_name(device, deviceName);
            if (error != Error.NO_ERROR)
            {
                throw new TobiiException("Failed to get device name: " + error);
            }
            return deviceName.ToString();
        }

        public static void SetLogger(ILogger logger)
        {
            Log = (message) => { logger.LogTrace(message); };
        }
    }
}
