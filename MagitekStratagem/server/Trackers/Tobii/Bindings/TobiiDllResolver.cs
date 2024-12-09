using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings;

internal class TobiiDllResolver
{
    private ILoggerFactory LoggerFactory;
    private ILogger logger;

    public TobiiDllResolver(ILoggerFactory loggerFactory)
    {
        this.LoggerFactory = loggerFactory;
        this.logger = loggerFactory.CreateLogger<TobiiDllResolver>();
    }

    public IntPtr ResolveTobiiGameIntegrationDll()
    {
        var tobiiDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TobiiGameHub");
        if (!Path.Exists(tobiiDir))
        {
            throw new TobiiGameHubNotFoundException();
        }
        var tobiiPath = LocateNewestTobiiGameHub(tobiiDir);

        if (tobiiPath != null)
        {
            logger.LogTrace($"Searching potential Tobii GameHub install path: {tobiiPath}");
            if (Path.Exists(tobiiPath))
            {
                var lib = Path.Join(tobiiPath, StreamEngine.Library);
                logger.LogTrace("Loading Tobii Stream Engine DLL from " + lib);
                try
                {
                    return NativeLibrary.Load(lib);
                }
                catch (Exception e)
                {
                    logger.LogError("Failed to load Tobii Stream Engine DLL: " + e.Message);
                }
            }
            else
            {
                throw new TobiiGameIntegrationNotFoundException();
            }
        }
        else
        {
            logger.LogTrace("Tobii GameHub not found.");
        }

        return IntPtr.Zero;
    }

    public string LocateNewestTobiiGameHub(string dirPath, Version? force = null)
    {
        var regex = new Regex(@"app-([0-9]+\.[0-9]+\.[0-9])-?.*");
        var dirs = Directory.GetDirectories(dirPath);
        var highestVersion = new Version(0, 0, 0);
        var highestVersionDir = "";
        foreach (var dir in dirs)
        {
            var dirName = Path.GetFileName(dir);
            var versPart = regex.Match(dirName).Groups[1].Value;
            if (Version.TryParse(versPart, out var version))
            {
                logger.LogTrace($"Found Tobii GameHub version {version}");
                if (force == null && version > highestVersion)
                {
                    highestVersion = version;
                    highestVersionDir = dir;
                }
                else if (force != null && version == force)
                {
                    return dir;
                }
            }
        }
        return highestVersionDir;
    }
}
