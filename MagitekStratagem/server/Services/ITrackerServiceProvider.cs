using MagitekStratagemServer.Trackers;

namespace MagitekStratagemServer.Services
{

    public interface ITrackerServiceProvider
    {
        ITrackerService? GetTracker(string fullName);

        IEnumerable<Type> ListTrackers();
    }
}
