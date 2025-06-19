namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
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
}
