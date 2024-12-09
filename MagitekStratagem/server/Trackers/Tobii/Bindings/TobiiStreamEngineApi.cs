using System.Runtime.InteropServices;
using System.Text;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    public static class StreamEngine
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

        public enum FieldOfUse
        {
            INTERACTIVE = 1,
            ANALYTICAL = 2,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Version
        {
            public int major;
            public int minor;
            public int revision;
            public int build;
        }

        public enum Validity
        {
            INVALID = 0,
            VALID = 1
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Vector2D
        {
            public float x;
            public float y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GazePoint
        {
            public long timestamp;
            public Validity validity;
            public Vector2D position;
        }

        private static Action<string> Log = (message) => { Console.WriteLine("StreamEngine: " + message); };

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void url_receiver(string url, nint user_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GazePointCallback(ref GazePoint gaze_point, nint user_data);

        public const string Library = "tobii_stream_engine.dll";

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_get_api_version")]
        private static extern Error get_api_version(out Version version);

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

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl,
                    EntryPoint = "tobii_wait_for_callbacks")]
        private static extern Error wait_for_callbacks(int device_count, nint[] devices);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl,
                    EntryPoint = "tobii_device_process_callbacks")]
        private static extern Error device_process_callbacks(nint device);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "tobii_get_device_name")]
        private static extern Error get_device_name(nint device, StringBuilder device_name);

        public static Version GetApiVersion()
        {
            Version version;
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

    public abstract class UnmanagedObject : IDisposable
    {
        public nint Ptr { get; private set; }

        protected UnmanagedObject(nint ptr)
        {
            Ptr = ptr;
        }

        protected abstract void Destroy();

        protected virtual void Dispose(bool disposing)
        {
            if (Ptr != nint.Zero)
            {
                Destroy();
                Ptr = nint.Zero;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class Api : UnmanagedObject
    {
        protected override void Destroy()
        {
            StreamEngine.DestroyApi(Ptr);
        }

        public Api(nint ptr) : base(ptr)
        {
        }

        public List<string> EnumerateDeviceUrls()
        {
            return StreamEngine.EnumerateLocalDevices(Ptr);
        }

        public Device CreateDevice(string url)
        {
            return StreamEngine.CreateDevice(Ptr, url);
        }

        public void WaitForCallbacks(IEnumerable<Device> devices)
        {
            StreamEngine.WaitForCallbacks(devices.Select((device) => device.Ptr));
        }
    }

    public class Device : UnmanagedObject
    {
        private Thread? processingThread;

        private readonly StreamEngine.GazePointCallback gazePointCallback;

        public Device(nint ptr) : base(ptr)
        {
            Name = StreamEngine.GetDeviceName(Ptr);
            gazePointCallback = GazePointCallback;
        }

        public float GazeX { get; private set; }
        public float GazeY { get; private set; }
        public long GazeTimestamp { get; private set; }

        public string Name { get; private set; }

        public bool Subscribed { get; private set; }

        protected override void Destroy()
        {
            Unsubscribe();
            StreamEngine.DestroyDevice(Ptr);
        }

        public void Subscribe(bool threaded = true)
        {
            if (!Subscribed)
            {
                StreamEngine.GazePointSubscribe(Ptr, gazePointCallback);

                if (threaded)
                {
                    processingThread = new Thread(() =>
                    {
                        while (Subscribed)
                        {
                            StreamEngine.WaitForCallbacks(new[] { Ptr });
                            StreamEngine.ProcessCallbacks(Ptr);
                            Thread.Sleep(1);
                        }
                    });

                    processingThread.Start();
                }

                Subscribed = true;
            }
        }

        public void Unsubscribe()
        {
            if (Subscribed)
            {
                StreamEngine.GazePointUnsubscribe(Ptr);
                Subscribed = false;
                processingThread?.Join();
            }
        }

        public void ProcessCallbacks()
        {
            StreamEngine.ProcessCallbacks(Ptr);
        }

        public override string ToString()
        {
            return $"Device {Name} ({Ptr})";
        }

        private void GazePointCallback(ref StreamEngine.GazePoint gazePoint, nint userData = default)
        {
            if (gazePoint.validity != StreamEngine.Validity.VALID)
            {
                return;
            }

            if (gazePoint.timestamp <= GazeTimestamp)
            {
                return;
            }

            GazeX = gazePoint.position.x;
            GazeY = gazePoint.position.y;
            GazeTimestamp = gazePoint.timestamp;
        }
    }

    public class TobiiException : Exception
    {
        public TobiiException(string message) : base(message) { }
    }
}
