namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
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

        private void GazePointCallback(ref GazePoint gazePoint, nint userData = default)
        {
            if (gazePoint.validity != Validity.VALID)
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
}
