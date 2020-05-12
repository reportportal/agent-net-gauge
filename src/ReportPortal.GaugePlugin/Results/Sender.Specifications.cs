using Gauge.Messages;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ReportPortal.GaugePlugin.Results
{
    partial class Sender
    {
        private ConcurrentDictionary<string, ITestReporter> _specs = new ConcurrentDictionary<string, ITestReporter>();

        public void StartSpec(SpecExecutionStartingRequest request)
        {
            lock (_lockObj)
            {
                if (_launch == null)
                {
                    var launchReporter = new LaunchReporter(_service, _configuration, null);

                    // if execution is rerun
                    if (request.CurrentExecutionInfo.ExecutionArgs.Any(arg => arg.FlagName.ToLowerInvariant() == "failed"))
                    {
                        _startLaunchRequest.IsRerun = true;
                    }

                    launchReporter.Start(_startLaunchRequest);

                    _launch = launchReporter;
                }

                var specResult = request.SpecResult;

                var specReporter = _launch.StartChildTestReporter(new StartTestItemRequest
                {
                    Type = TestItemType.Suite,
                    Name = specResult.ProtoSpec.SpecHeading,
                    Description = string.Join("", specResult.ProtoSpec.Items.Where(i => i.ItemType == ProtoItem.Types.ItemType.Comment).Select(c => c.Comment.Text)),
                    StartTime = DateTime.UtcNow,
                    Attributes = specResult.ProtoSpec.Tags.Select(t => new ItemAttribute { Value = t.ToString() }).ToList()
                });

                var key = GetSpecKey(request.CurrentExecutionInfo.CurrentSpec);
                _specs[key] = specReporter;
            }
        }

        public void FinishSpec(SpecExecutionEndingRequest request)
        {
            var key = GetSpecKey(request.CurrentExecutionInfo.CurrentSpec);

            _specs[key].Finish(new FinishTestItemRequest
            {
                EndTime = DateTime.UtcNow
            });

            _specs.TryRemove(key, out _);
        }

        private string GetSpecKey(SpecInfo specInfo)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { specInfo.Name, specInfo.FileName });
        }
    }
}
