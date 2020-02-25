using Gauge.Messages;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReportPortal.GaugePlugin.Results
{
    partial class Sender
    {
        private ILaunchReporter _launchReporter;

        public void StartLaunch(ExecutionStartingRequest request)
        {
            var suiteExecutionResult = request.SuiteResult;

            _launchReporter = new LaunchReporter(_service, _configuration, null);
            _launchReporter.Start(new StartLaunchRequest
            {
                Name = _configuration.GetValue("Launch:Name", suiteExecutionResult.ProjectName),
                Description = _configuration.GetValue("Launch:Description", string.Empty),
                Tags = _configuration.GetValues("Launch:Tags", new List<string>()).ToList(),
                StartTime = DateTime.UtcNow
            });
        }

        public void FinishLaunch(ExecutionEndingRequest request)
        {
            _launchReporter.Finish(new FinishLaunchRequest
            {
                EndTime = DateTime.UtcNow
            });
        }

        public void Sync()
        {
            _launchReporter.Sync();
        }
    }
}
