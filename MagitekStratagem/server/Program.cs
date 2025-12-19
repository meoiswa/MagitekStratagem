
using System.Runtime.InteropServices;
using MagitekStratagemServer.Hubs;
using MagitekStratagemServer.Services;
using MagitekStratagemServer.Trackers.Eyeware.Bindings;
using MagitekStratagemServer.Trackers.Tobii;
using MagitekStratagemServer.Trackers.Tobii.Bindings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MagitekStratagemServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddSignalR();
            builder.Services.Configure<JsonHubProtocolOptions>(options =>
            {
                options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
            });

            builder.Services.AddSingleton<ITrackerServiceProvider, TrackerServiceProvider>();
            builder.Services.AddSingleton<TobiiDllResolver>();
            builder.Services.AddSingleton<EyewareDllResolver>();

            var app = builder.Build();

            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var tobiiDllResolver = app.Services.GetRequiredService<TobiiDllResolver>();
            var eyewareDllResolver = app.Services.GetRequiredService<EyewareDllResolver>();

            NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, (libraryName, assembly, searchPath) =>
            {
                logger.LogTrace($"Resolving {libraryName} for {assembly.FullName} from {searchPath}");
                if (libraryName == StreamEngine.Library)
                {
                    return tobiiDllResolver.ResolveTobiiGameIntegrationDll();
                }
                else if (libraryName == TrackerClient.Library)
                {
                    return eyewareDllResolver.ResolveEyewareDll();
                }
                return IntPtr.Zero;
            });

            app.UseDefaultFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseStaticFiles();
            app.MapHub<MagitekStratagemHub>("/hub");

            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var skipAutoShutdown = false;
            // Use environment variable to skip shutdown timer
            var debugEnv = Environment.GetEnvironmentVariable("MAGITEKSTRATAGEM_DEBUG");
            if (!string.IsNullOrEmpty(debugEnv) && debugEnv == "1")
            {
                skipAutoShutdown = true;
                logger.LogInformation("MAGITEKSTRATAGEM_DEBUG is set, skipping server auto-shutdown.");
            }

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000, token);
                    // Check if there are any clients connected
                    if (MagitekStratagemHub.ConnectedClients <= 0 && !skipAutoShutdown)
                    {
                        for (int i = 10; i >= 0; i--)
                        {
                            logger.LogInformation($"No clients connected, shutting down server in {i} seconds.");
                            await Task.Delay(1000, token);
                            if (MagitekStratagemHub.ConnectedClients > 0)
                            {
                                logger.LogInformation("Aborted server shutdown.");
                                break;
                            }
                        }
                        if (MagitekStratagemHub.ConnectedClients <= 0)
                        {
                            logger.LogInformation("Server shutting down...");
                            cts.Cancel();
                        }
                    }
                }

                // terminate the server
                app.StopAsync().Wait();
            }, token);

            app.Run();
        }
    }
}
