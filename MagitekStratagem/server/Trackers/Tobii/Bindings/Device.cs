using System.Numerics;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    public class Device : UnmanagedObject
    {
        private Thread? processingThread;

        private readonly StreamEngine.GazePointCallback gazePointCallback;

        private readonly StreamEngine.HeadPoseCallback headPoseCallback;

        public Device(nint ptr) : base(ptr)
        {
            Name = StreamEngine.GetDeviceName(Ptr);
            gazePointCallback = GazePointCallback;
            headPoseCallback = HeadPoseCallback;
        }

        public long LastGazeTimestamp { get; private set; }
        public Vector2 LastGazePoint { get; private set; }


        public long LastHeadTimestamp { get; private set; }
        public Vector3 LastHeadPosition { get; private set; }
        public Vector3 LastHeadRotation { get; private set; }

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
                StreamEngine.HeadPoseSubscribe(Ptr, headPoseCallback);

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
                StreamEngine.HeadPoseUnsubscribe(Ptr);
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

        private void GazePointCallback(ref GazePoint gazePoint, nint userData = default)
        {
            if (gazePoint.validity != Validity.VALID)
            {
                return;
            }

            if (gazePoint.timestamp <= LastGazeTimestamp)
            {
                return;
            }

            LastGazePoint = new Vector2(gazePoint.position.x * 2 - 1, -(gazePoint.position.y * 2 - 1));
            LastGazeTimestamp = gazePoint.timestamp;
        }

        private void HeadPoseCallback(ref HeadPose headPose, nint userData = default)
        {
            if (headPose.position_validity != Validity.VALID)
            {
                return;
            }

            if (headPose.timestamp <= LastHeadTimestamp)
            {
                return;
            }

            LastHeadPosition = new Vector3(headPose.position_xyz.x, headPose.position_xyz.y, headPose.position_xyz.z);
            LastHeadRotation = new Vector3(headPose.rotation_xyz.x, headPose.rotation_xyz.y, -headPose.rotation_xyz.z) * (180f / MathF.PI);
            LastHeadTimestamp = headPose.timestamp;
        }
    }
}
