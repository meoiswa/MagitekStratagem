
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;
using MagitekStratagemServer.Attributes;
using MagitekStratagemServer.Trackers.OpentrackUdp.Bindings;

namespace MagitekStratagemServer.Trackers.OpentrackUdp
{
    [TrackerService("Opentrack UDP")]
    internal class OpentrackUdpService : BaseTrackerService
    {
        private UdpClient? _udpClient;
        private Thread? _listenerThread;
        private bool _listening;
        private readonly object _lock = new();
        private const int OpentrackPort = 4242; // Default opentrack UDP port
        private OpentrackUdpHeadPose _lastPose;

        public OpentrackUdpService(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        public override void DoDispose()
        {
            StopListener();
        }

        public override void DoStartTracking()
        {
            IsTracking = true;
            StartListener();
        }

        public override void DoStopTracking()
        {
            IsTracking = false;
            StopListener();
        }

        protected override void DoUpdate()
        {
            if (!IsTracking)
            {
                return;
            }
            lock (_lock)
            {
                LastGazeTimestamp = DateTime.Now.Ticks;
                LastGazeX = (float)(_lastPose.Yaw / 90d); // Normalize to -1 to 1 range
                LastGazeY = (float)(_lastPose.Pitch / 90d); // Normalize to -1 to 1 range
            }
        }

        private void StartListener()
        {
            if (_listening)
            {
                return;
            }
            _listening = true;
            _udpClient = new UdpClient(OpentrackPort);
            _listenerThread = new Thread(ListenLoop) { IsBackground = true };
            _listenerThread.Start();
        }

        private void StopListener()
        {
            _listening = false;
            try
            {
                if (_udpClient != null)
                {
                    _udpClient.Close();
                }
            }
            catch { }
            _udpClient = null;
            if (_listenerThread != null && _listenerThread.IsAlive)
            {
                try { _listenerThread.Join(500); } catch { }
            }
            _listenerThread = null;
        }

        private void ListenLoop()
        {
            try
            {
                while (_listening)
                {
                    if (_udpClient == null)
                    {
                        break;
                    }
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _udpClient.Receive(ref remoteEP);
                    if (data.Length >= Marshal.SizeOf<OpentrackUdpHeadPose>())
                    {
                        OpentrackUdpHeadPose pose;
                        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                        try
                        {
                            pose = Marshal.PtrToStructure<OpentrackUdpHeadPose>(handle.AddrOfPinnedObject());
                        }
                        finally
                        {
                            handle.Free();
                        }
                        lock (_lock)
                        {
                            _lastPose = pose;
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            catch (Exception) { }
        }
    }
}
