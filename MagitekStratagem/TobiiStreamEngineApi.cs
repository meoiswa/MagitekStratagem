using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Tobii2
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

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void url_receiver(string url, IntPtr user_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void GazePointCallback(ref GazePoint gaze_point, IntPtr user_data);

    public const string Library = "tobii_stream_engine.dll";

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_get_api_version")]
    private static extern Error get_api_version(out Version version);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_api_create")]
    private static extern Error api_create(out IntPtr api, IntPtr custom_alloc, IntPtr custom_log);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_api_destroy")]
    private static extern Error api_destroy(IntPtr api);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "tobii_enumerate_local_device_urls")]
    private static extern Error enumerate_local_device_urls(IntPtr api, url_receiver receiver, IntPtr userData);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "tobii_device_create")]
    private static extern Error device_create(IntPtr api, string url, FieldOfUse fieldOfUse, out IntPtr device);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "tobii_device_destroy")]
    private static extern Error device_destroy(IntPtr device);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_gaze_point_subscribe")]
    private static extern Error gaze_point_subscribe(IntPtr device, GazePointCallback callback, IntPtr user_data);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "tobii_gaze_point_unsubscribe")]
    private static extern Error gaze_point_unsubscribe(IntPtr device);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "tobii_wait_for_callbacks")]
    private static extern Error wait_for_callbacks(int device_count, IntPtr[] devices);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl,
                EntryPoint = "tobii_device_process_callbacks")]
    private static extern Error device_process_callbacks(IntPtr device);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "tobii_get_device_name")]
    private static extern Error get_device_name(IntPtr device, StringBuilder device_name);

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
      IntPtr ptr;
      var error = api_create(out ptr, IntPtr.Zero, IntPtr.Zero);
      if (error != Error.NO_ERROR)
      {
        throw new TobiiException("Failed to create API: " + error);
      }
      return new Api(ptr);
    }

    public static void DestroyApi(IntPtr api)
    {
      Console.WriteLine("Disposing of Api " + api);
      var error = api_destroy(api);
      if (error != Error.NO_ERROR)
      {
        throw new TobiiException("Failed to destroy API: " + error);
      }
    }

    public static List<string> EnumerateLocalDevices(IntPtr api)
    {
      var urls = new List<string>();
      url_receiver receiver = (url, userData) => urls.Add(url);
      var error = enumerate_local_device_urls(api, receiver, IntPtr.Zero);
      if (error != Error.NO_ERROR)
      {
        throw new TobiiException("Failed to enumerate local devices: " + error);
      }
      return urls;
    }

    public static Device CreateDevice(IntPtr api, string url)
    {
      IntPtr ptr;
      var error = device_create(api, url, FieldOfUse.INTERACTIVE, out ptr);
      if (error != Error.NO_ERROR)
      {
        throw new TobiiException("Failed to create device: " + error);
      }
      return new Device(ptr);
    }

    public static void DestroyDevice(IntPtr device)
    {
      Console.WriteLine("Disposing of Device " + device);
      var error = device_destroy(device);
      if (error != Error.NO_ERROR)
      {
        throw new TobiiException("Failed to destroy device: " + error);
      }
    }

    public static void GazePointSubscribe(IntPtr device, GazePointCallback callback)
    {
      var error = gaze_point_subscribe(device, callback, IntPtr.Zero);
      if (error != Error.NO_ERROR)
      {
        throw new TobiiException("Failed to subscribe to gaze point: " + error);
      }
    }

    public static void GazePointUnsubscribe(IntPtr device)
    {
      Console.WriteLine("Unsusbcribing from Gaze Point " + device);
      var error = gaze_point_unsubscribe(device);
      if (error != Error.NO_ERROR)
      {
        throw new TobiiException("Failed to unsubscribe from gaze point: " + error);
      }
    }

    public static bool WaitForCallbacks(IEnumerable<IntPtr> devices)
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

    public static void ProcessCallbacks(IntPtr device)
    {
      if (device != IntPtr.Zero)
      {
        var error = device_process_callbacks(device);
        if (error != Error.NO_ERROR)
        {
          throw new TobiiException("Failed to process callbacks: " + error);
        }
      }
    }

    public static string GetDeviceName(IntPtr device)
    {
      var deviceName = new StringBuilder(256);
      var error = get_device_name(device, deviceName);
      if (error != Error.NO_ERROR)
      {
        throw new TobiiException("Failed to get device name: " + error);
      }
      return deviceName.ToString();
    }
  }

  public abstract class UnmanagedObject : IDisposable
  {
    public IntPtr Ptr { get; private set; }

    protected UnmanagedObject(IntPtr ptr)
    {
      Ptr = ptr;
    }

    protected abstract void Destroy();

    protected virtual void Dispose(bool disposing)
    {
      if (Ptr != IntPtr.Zero)
      {
        Destroy();
        Console.WriteLine("Disposed of Unmanaged Object " + this.GetType());
        Ptr = IntPtr.Zero;
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

    public Api(IntPtr ptr) : base(ptr)
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

    public Device(IntPtr ptr) : base(ptr)
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

    private void GazePointCallback(ref StreamEngine.GazePoint gazePoint, IntPtr userData = default)
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
