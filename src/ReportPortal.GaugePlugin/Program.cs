using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReportPortal.Shared.Internal.Logging;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ReportPortal.GaugePlugin
{
    class Program
    {
        private static ITraceLogger TraceLogger;

        public static CancellationTokenSource ShutDownCancelationSource = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            var gaugeProjectRoot = Environment.GetEnvironmentVariable("GAUGE_PROJECT_ROOT") ?? Environment.CurrentDirectory;
            var gaugeLogsDir = Environment.GetEnvironmentVariable("logs_directory") ?? "logs";

            var internalTraceLogginDir = Path.Combine(gaugeProjectRoot, gaugeLogsDir);

            TraceLogger = TraceLogManager.Instance.WithBaseDir(internalTraceLogginDir).GetLogger<Program>();

            var envVariables = Environment.GetEnvironmentVariables();
            foreach (var envVariableKey in envVariables.Keys)
            {
                TraceLogger.Verbose($"{envVariableKey}: {envVariables[envVariableKey]}");
            }

            using (var host = CreateHostBuilder(args).Build())
            {
                await host.StartAsync();

                await host.WaitForShutdownAsync(ShutDownCancelationSource.Token);
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder.ConfigureLogging(logging => logging.ClearProviders());

                   webBuilder.UseKestrel(options =>
                   {
                       options.Listen(IPAddress.Loopback,
                           0,
                           listenOptions => listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
                   })
                   .UseStartup<Startup>();
               });
    }
}
