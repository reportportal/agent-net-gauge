using Gauge.Messages;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportPortal.GaugePlugin.Results
{
    partial class Sender
    {
        private readonly object _lockObj = new();

        private int _launchesCount;

        private ILaunchReporter _launch;

        private StartLaunchRequest _startLaunchRequest;

        public void StartLaunch(ExecutionStartingRequest request)
        {
            lock (_lockObj)
            {
                if (_launch == null)
                {
                    var suiteExecutionResult = request.SuiteResult;
                    var isDebug = _configuration.GetValue("Launch:DebugMode", false);

                    // deffer starting of launch
                    _startLaunchRequest = new StartLaunchRequest
                    {
                        Name = _configuration.GetValue("Launch:Name", suiteExecutionResult.ProjectName),
                        Description = _configuration.GetValue("Launch:Description", string.Empty),
                        Attributes = _configuration.GetKeyValues("Launch:Attributes", new List<KeyValuePair<string, string>>()).Select(a => new Client.Abstractions.Models.ItemAttribute { Key = a.Key, Value = a.Value }).ToList(),
                        Mode = isDebug ? LaunchMode.Debug : LaunchMode.Default,
                        StartTime = DateTime.UtcNow                        
                    };
                }

                _launchesCount++;
            }
        }

        public void FinishLaunch(ExecutionEndingRequest request)
        {
            lock (_lockObj)
            {
                _launchesCount--;

                if (_launchesCount == 0)
                {
                    _launch.Finish(new FinishLaunchRequest
                    {
                        EndTime = DateTime.UtcNow
                    });
                }
            }
        }

        public void Sync()
        {
            lock (_lockObj)
            {
                if (_launchesCount == 0)
                {
                    if (_launch is null)
                    {
                        throw new NullReferenceException("Launch cannot be null. Check logs folder to find potential problems.");
                    }
                    else
                    {
                        _launch.Sync();
                    }
                }
            }
        }

        public ILaunchReporter LaunchReporter => _launch;

    }
}
