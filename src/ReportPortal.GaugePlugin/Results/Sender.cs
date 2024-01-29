using Gauge.Messages;
using ReportPortal.Client.Abstractions;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Internal.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReportPortal.GaugePlugin.Results
{
    partial class Sender
    {
        private static readonly ITraceLogger TraceLogger = TraceLogManager.Instance.GetLogger<Sender>();

        private static readonly Dictionary<ExecutionStatus, Status> _statusMap;

        private readonly string _gaugeScreenshotsDir;

        private readonly IClientService _service;
        private readonly IConfiguration _configuration;

        static Sender()
        {
            _statusMap = new Dictionary<ExecutionStatus, Status>
            {
                { ExecutionStatus.Failed, Status.Failed },
                { ExecutionStatus.Notexecuted, Status.Skipped },
                { ExecutionStatus.Passed, Status.Passed },
                { ExecutionStatus.Skipped, Status.Skipped }
            };

            Shared.Extensibility.Embedded.Analytics.AnalyticsReportEventsObserver.DefineConsumer("agent-dotnet-gauge", Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
        }

        public Sender(IClientService service, IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;

            _gaugeScreenshotsDir = Environment.GetEnvironmentVariable("gauge_screenshots_dir");
        }
    }
}
