using System.Runtime.InteropServices;

namespace MagitekStratagemServer.Trackers.Eyeware.Bindings;

internal class EyewareDllResolver
{
    private ILoggerFactory LoggerFactory;
    private ILogger logger;

    public EyewareDllResolver(ILoggerFactory loggerFactory)
    {
        this.LoggerFactory = loggerFactory;
        this.logger = loggerFactory.CreateLogger<EyewareDllResolver>();
    }

    public IntPtr ResolveEyewareDll()
    {
        logger.LogTrace($"Searching for Eyeware Tracker Client DLL...");
        var runDir = Path.GetDirectoryName(AppContext.BaseDirectory);

        if (runDir != null)
        {
            var lib = Path.Join(runDir, "server", "Trackers", "Eyeware", "lib", TrackerClient.Library);
            logger.LogTrace("Loading Eyeware Tracker Client DLL from " + lib);
            try
            {
                return NativeLibrary.Load(lib);
            }
            catch (Exception e)
            {
                logger.LogError("Failed to load Eyeware Tracker Client DLL: " + e.Message);
            }
        }
        else
        {
            logger.LogTrace("Eyeware Tracker Client DLL not found.");
        }

        return IntPtr.Zero;
    }
}
