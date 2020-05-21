using Gauge.Messages;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Concurrent;
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
                    var launchReporter = new LaunchReporter(_service, _configuration, null, new ExtensionManager());

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

                // pre hook messages
                if (specResult.ProtoSpec.PreHookMessages.Count != 0 || specResult.ProtoSpec.PreHookFailures.Count != 0)
                {
                    var preHookReporter = specReporter.StartChildTestReporter(new StartTestItemRequest
                    {
                        Name = "Before Specification",
                        StartTime = DateTime.UtcNow,
                        Type = TestItemType.BeforeClass
                    });

                    foreach (var preHookMessage in specResult.ProtoSpec.PreHookMessages)
                    {
                        preHookReporter.Log(new CreateLogItemRequest
                        {
                            Level = LogLevel.Debug,
                            Text = preHookMessage,
                            Time = DateTime.UtcNow
                        });
                    }

                    foreach (var preHookFailure in specResult.ProtoSpec.PreHookFailures)
                    {
                        preHookReporter.Log(new CreateLogItemRequest
                        {
                            Level = LogLevel.Error,
                            Text = $"{preHookFailure.ErrorMessage}{Environment.NewLine}{preHookFailure.StackTrace}",
                            Time = DateTime.UtcNow
                        });
                    }

                    preHookReporter.Finish(new FinishTestItemRequest
                    {
                        EndTime = DateTime.UtcNow,
                        Status = specResult.ProtoSpec.PreHookFailures.Count == 0 ? Status.Passed : Status.Failed

                    });
                }

                var key = GetSpecKey(request.CurrentExecutionInfo, request.CurrentExecutionInfo.CurrentSpec);
                _specs[key] = specReporter;
            }
        }

        public void FinishSpec(SpecExecutionEndingRequest request)
        {
            var key = GetSpecKey(request.CurrentExecutionInfo, request.CurrentExecutionInfo.CurrentSpec);

            var specReporter = _specs[key];

            var specResult = request.SpecResult;

            // post hook messages
            if (specResult.ProtoSpec.PostHookMessages.Count != 0 || specResult.ProtoSpec.PostHookFailures.Count != 0)
            {
                var postHookReporter = specReporter.StartChildTestReporter(new StartTestItemRequest
                {
                    Name = "After Specification",
                    StartTime = DateTime.UtcNow,
                    Type = TestItemType.AfterClass
                });

                foreach (var postHookMessage in specResult.ProtoSpec.PostHookMessages)
                {
                    postHookReporter.Log(new CreateLogItemRequest
                    {
                        Level = LogLevel.Debug,
                        Text = postHookMessage,
                        Time = DateTime.UtcNow
                    });
                }

                foreach (var postHookFailure in specResult.ProtoSpec.PostHookFailures)
                {
                    postHookReporter.Log(new CreateLogItemRequest
                    {
                        Level = LogLevel.Error,
                        Text = $"{postHookFailure.ErrorMessage}{Environment.NewLine}{postHookFailure.StackTrace}",
                        Time = DateTime.UtcNow
                    });
                }

                postHookReporter.Finish(new FinishTestItemRequest
                {
                    EndTime = DateTime.UtcNow,
                    Status = specResult.ProtoSpec.PostHookFailures.Count == 0 ? Status.Passed : Status.Failed

                });
            }

            specReporter.Finish(new FinishTestItemRequest
            {
                EndTime = DateTime.UtcNow
            });

            _specs.TryRemove(key, out _);
        }

        private string GetSpecKey(ExecutionInfo executionInfo, SpecInfo specInfo)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { specInfo.Name, specInfo.FileName, executionInfo.RunnerId });
        }
    }
}
