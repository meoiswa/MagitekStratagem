namespace MagitekStratagemServer.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TrackerServiceAttribute : Attribute
    {
        public TrackerServiceAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
