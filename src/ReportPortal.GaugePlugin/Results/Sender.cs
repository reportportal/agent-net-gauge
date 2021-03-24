using Gauge.Messages;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Internal.Logging;
using System;
using System.Collections.Generic;

namespace ReportPortal.GaugePlugin.Results
{
    partial class Sender
    {
        private static ITraceLogger TraceLogger = TraceLogManager.Instance.GetLogger<Sender>();

        private static Dictionary<ExecutionStatus, Status> _statusMap;

        private string _gaugeScreenshotsDir;

        private IClientService _service;
        private IConfiguration _configuration;

        static Sender()
        {
            _statusMap = new Dictionary<ExecutionStatus, Status>
            {
                { ExecutionStatus.Failed, Status.Failed },
                { ExecutionStatus.Notexecuted, Status.Skipped },
                { ExecutionStatus.Passed, Status.Passed },
                { ExecutionStatus.Skipped, Status.Skipped }
            };

            Shared.Extensibility.Analytics.AnalyticsReportEventsObserver.DefineConsumer("agent-dotnet-gauge");
        }

        public Sender(IClientService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;

            _gaugeScreenshotsDir = Environment.GetEnvironmentVariable("gauge_screenshots_dir");
        }
    }
}
