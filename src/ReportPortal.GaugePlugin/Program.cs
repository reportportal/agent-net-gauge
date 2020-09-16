using Gauge.Messages;
using Grpc.Core;
using ReportPortal.Client;
using ReportPortal.GaugePlugin.Results;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Configuration.Providers;
using ReportPortal.Shared.Internal.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ReportPortal.GaugePlugin
{
    class Program
    {
        private static ITraceLogger TraceLogger;

        static async Task Main(string[] args)
        {
            var gaugeProjectRoot = Environment.GetEnvironmentVariable("GAUGE_PROJECT_ROOT");
            var gaugeLogsDir = Environment.GetEnvironmentVariable("logs_directory");

            var internalTraceLogginDir = Path.Combine(gaugeProjectRoot, gaugeLogsDir);

            TraceLogger = TraceLogManager.Instance.WithBaseDir(internalTraceLogginDir).GetLogger<Program>();

            var envVariables = Environment.GetEnvironmentVariables();
            foreach (var envVariableKey in envVariables.Keys)
            {
                TraceLogger.Verbose($"{envVariableKey}: {envVariables[envVariableKey]}");
            }

            var configuration = new ConfigurationBuilder()
                .Add(new EnvironmentVariablesConfigurationProvider("RP_", "_", EnvironmentVariableTarget.Process))
                .Build();

            var rpUri = configuration.GetValue<string>("Uri");
            var rpProject = configuration.GetValue<string>("Project");
            var rpApiToken = configuration.GetValue<string>("Uuid");
            var apiClientService = new Service(new Uri(rpUri), rpProject, rpApiToken);

            var sender = new Sender(apiClientService, configuration);

            var channelOptions = new List<ChannelOption> {
                new ChannelOption(ChannelOptions.MaxReceiveMessageLength, -1)
            };

            var server = new Server(channelOptions);

            ServerServiceDefinition messagesHandlerService;
            if (configuration.GetValue("Enabled", true))
            {
                messagesHandlerService = Reporter.BindService(new ReportMessagesHandler(server, sender));
            }
            else
            {
                messagesHandlerService = Reporter.BindService(new EmptyMessagesHandler(server));
            }
            server.Services.Add(messagesHandlerService);

            var gaugePort = server.Ports.Add(new ServerPort("localhost", 0, ServerCredentials.Insecure));
            server.Start();

            Console.Write($"Listening on port:{gaugePort}");

            TraceLogger.Info("Server has started.");

            await server.ShutdownTask;
        }
    }
}
