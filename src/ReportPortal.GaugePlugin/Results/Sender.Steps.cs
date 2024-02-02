using Gauge.Messages;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.GaugePlugin.Extensions;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace ReportPortal.GaugePlugin.Results
{
    partial class Sender
    {
        private readonly ConcurrentDictionary<StepKey, ITestReporter> _steps = new();

        public void StartStep(StepExecutionStartingRequest request)
        {
            var stepResult = request.StepResult;

            var key = GetStepKey(request.CurrentExecutionInfo, request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario, request.CurrentExecutionInfo.CurrentStep);
            TraceLogger.Verbose($"Starting step with key: {key}");

            var scenarioKey = GetScenarioKey(request.CurrentExecutionInfo, request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario);

            var parentReporter = _scenarioConcepts.TryGetValue(scenarioKey, out System.Collections.Generic.List<ITestReporter> concept) ? concept.Last() : _scenarios[scenarioKey];

            var stepName = request.CurrentExecutionInfo.CurrentStep.Step.GetStepName();

            var stepReporter = parentReporter.StartChildTestReporter(new StartTestItemRequest
            {
                Type = TestItemType.Step,
                StartTime = DateTime.UtcNow,
                Name = stepName,
                HasStats = false
            });

            // if step argument is table
            var tableParameter = stepResult.ProtoItem.Step.Fragments.FirstOrDefault(f => f.Parameter?.Table != null)?.Parameter.Table;
            if (tableParameter != null)
            {
                stepReporter.Log(new CreateLogItemRequest
                {
                    Time = DateTime.UtcNow,
                    Level = LogLevel.Info,
                    Text = tableParameter.AsMarkdown()
                });
            }

            // pre hook messages
            if (stepResult.ProtoItem.Step.PreHookMessages.Count != 0)
            {
                foreach (var preHookMessage in stepResult.ProtoItem.Step.PreHookMessages)
                {
                    stepReporter.Log(new CreateLogItemRequest
                    {
                        Level = LogLevel.Debug,
                        Text = preHookMessage,
                        Time = DateTime.UtcNow
                    });
                }
            }

            _steps[key] = stepReporter;
        }

        public void FinishStep(StepExecutionEndingRequest request)
        {
            var key = GetStepKey(request.CurrentExecutionInfo, request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario, request.CurrentExecutionInfo.CurrentStep);
            TraceLogger.Verbose($"Finishing step with key: {key}");
            var stepReporter = _steps[key];

            var stepStatus = Status.Passed;

            // process gauge log messages
            var logMessages = request.StepResult.ProtoItem.Step.StepExecutionResult.ExecutionResult.Message;
            if (logMessages != null)
            {
                foreach (var logMessage in logMessages)
                {
                    stepReporter.Log(new CreateLogItemRequest
                    {
                        Text = logMessage,
                        Level = LogLevel.Debug,
                        Time = DateTime.UtcNow
                    });
                }
            }

            // todo it's never skipped
            if (request.StepResult.ProtoItem.Step.StepExecutionResult.Skipped)
            {
                stepStatus = Status.Skipped;

                stepReporter.Log(new CreateLogItemRequest
                {
                    Time = DateTime.UtcNow,
                    Level = LogLevel.Info,
                    Text = $"Skip reason: {request.StepResult.ProtoItem.Step.StepExecutionResult.SkippedReason}"
                });
            };

            if (request.StepResult.ProtoItem.Step.StepExecutionResult.ExecutionResult.Failed)
            {
                stepStatus = Status.Failed;

                stepReporter.Log(new CreateLogItemRequest
                {
                    Time = DateTime.UtcNow,
                    Level = LogLevel.Error,
                    Text = $"{request.StepResult.ProtoItem.Step.StepExecutionResult.ExecutionResult.ErrorMessage}{Environment.NewLine}{Environment.NewLine}{request.StepResult.ProtoItem.Step.StepExecutionResult.ExecutionResult.StackTrace}"
                });
            }

            // process custom screenshots
            var screenshots = request.StepResult.ProtoItem.Step.StepExecutionResult.ExecutionResult.ScreenshotFiles;
            if (screenshots != null)
            {
                foreach (var screeenshot in screenshots)
                {
                    AttachScreenshot(screeenshot, LogLevel.Info, stepReporter);
                }
            }

            // process screenshot on failure
            AttachScreenshot(request.StepResult.ProtoItem.Step.StepExecutionResult.ExecutionResult.FailureScreenshotFile, LogLevel.Error, stepReporter);

            // post hook messages
            if (request.StepResult.ProtoItem.Step.PostHookMessages.Count != 0)
            {
                foreach (var postHookMessage in request.StepResult.ProtoItem.Step.PostHookMessages)
                {
                    stepReporter.Log(new CreateLogItemRequest
                    {
                        Level = LogLevel.Debug,
                        Text = postHookMessage,
                        Time = DateTime.UtcNow
                    });
                }
            }

            stepReporter.Finish(new FinishTestItemRequest
            {
                EndTime = DateTime.UtcNow,
                Status = stepStatus
            });

            _steps.TryRemove(key, out _);
        }

        private StepKey GetStepKey(ExecutionInfo executionInfo, SpecInfo specInfo, ScenarioInfo scenarioInfo, StepInfo stepInfo)
        {
            return new StepKey(GetScenarioKey(executionInfo, specInfo, scenarioInfo), stepInfo.Step.ActualStepText);
        }

        record StepKey(ScenarioKey ScenarioKey, string Name);

        private void AttachScreenshot(String screenshotFile, LogLevel logLevel, ITestReporter stepReporter)
        {
            try
            {
                if (!string.IsNullOrEmpty(screenshotFile))
                {
                    stepReporter.Log(new CreateLogItemRequest
                    {
                        Time = DateTime.UtcNow,
                        Level = logLevel,
                        Text = "Screenshot",
                        Attach = new LogItemAttach
                        {
                            Name = "screenshot",
                            MimeType = Shared.MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(screenshotFile)),
                            Data = File.ReadAllBytes(Path.Combine(_gaugeScreenshotsDir, screenshotFile))
                        }
                    });
                }
            }
            catch (Exception exp)
            {
                TraceLogger.Error($"Couldn't parse step screenshot. {exp}");
            }
        }
    }
}
