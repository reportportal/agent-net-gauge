using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReportPortal.Shared.Internal.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace ReportPortal.GaugePlugin
{
    class Program
    {
        private static ITraceLogger TraceLogger { get; set; }

        public static CancellationTokenSource ShutDownCancellationSource { get; } = new();

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Observers.FileResultsObserver))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Shared.Extensibility.Embedded.Analytics.AnalyticsReportEventsObserver))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Shared.Extensibility.Embedded.Normalization.RequestNormalizer))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Shared.Extensibility.Embedded.LaunchArtifacts.LaunchArtifactsEventsObserver))]
        static async Task Main(string[] args)
        {
            var gaugeProjectRoot = Environment.GetEnvironmentVariable("GAUGE_PROJECT_ROOT");
            Environment.CurrentDirectory = gaugeProjectRoot;
            var gaugeLogsDir = Environment.GetEnvironmentVariable("logs_directory") ?? "";

            var internalTraceLogginDir = Path.Combine(gaugeProjectRoot, gaugeLogsDir);

            TraceLogger = TraceLogManager.Instance.WithBaseDir(internalTraceLogginDir).GetLogger<Program>();

            var envVariables = Environment.GetEnvironmentVariables();
            foreach (var envVariableKey in envVariables.Keys)
            {
                TraceLogger.Verbose($"{envVariableKey}: {envVariables[envVariableKey]}");
            }

            using var host = CreateHostBuilder(args).Build();

            await host.StartAsync();

            await host.WaitForShutdownAsync(ShutDownCancellationSource.Token);
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Error));

                   webBuilder.UseKestrel(options =>
                   {
                       options.Listen(IPAddress.Loopback,
                           0,
                           listenOptions => listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);

                       options.Limits.MaxRequestBodySize = long.MaxValue;
                   })
                   .UseStartup<Startup>();
               });
    }
}
