using Gauge.Messages;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Converters;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ReportPortal.GaugePlugin.Results
{
    partial class Sender
    {
        private ConcurrentDictionary<string, ITestReporter> _scenarios = new ConcurrentDictionary<string, ITestReporter>();

        public void StartScenario(ScenarioExecutionStartingRequest request)
        {
            var scenarioResult = request.ScenarioResult.ProtoItem;

            ProtoScenario scenario;

            switch (scenarioResult.ItemType)
            {
                case ProtoItem.Types.ItemType.Scenario:
                    scenario = scenarioResult.Scenario;
                    break;
                case ProtoItem.Types.ItemType.TableDrivenScenario:
                    scenario = scenarioResult.TableDrivenScenario.Scenario;
                    break;
                default:
                    scenario = scenarioResult.Scenario;
                    break;
            }

            var specReporter = _specs[GetSpecKey(request.CurrentExecutionInfo, request.CurrentExecutionInfo.CurrentSpec)];

            // find TestCaseId
            var testCaseIdTagPrefix = "TestCaseId:";
            string testCaseIdTagValue = null;
            var testCaseIdTag = scenario.Tags.FirstOrDefault(t => t.ToLowerInvariant().StartsWith(testCaseIdTagPrefix.ToLowerInvariant()));
            if (testCaseIdTag != null)
            {
                testCaseIdTagValue = testCaseIdTag.Substring(testCaseIdTagPrefix.Length);
            }

            // parse scenario parameters
            var scenarioRequestParameters = new List<KeyValuePair<string, string>>();
            var scenarioStepFragments = scenarioResult.Scenario.ScenarioItems?
                .Where(si => si.ItemType == ProtoItem.Types.ItemType.Step && si.Step != null && si.Step.Fragments != null)
                .SelectMany(si => si.Step.Fragments.Where(f => f.FragmentType == Fragment.Types.FragmentType.Parameter));

            foreach (var scenarioStepFragment in scenarioStepFragments)
            {
                var paramName = scenarioStepFragment.Parameter.Name;
                var paramValue = scenarioStepFragment.Parameter.Value;
                scenarioRequestParameters.Add(new KeyValuePair<string, string>(paramName, paramValue));
            }

            var attributes = scenario.Tags.Select(t => new ItemAttributeConverter().ConvertFrom(t, opts => opts.UndefinedKey = "tag")).ToList();
            // inherit scenario tags from specification
            attributes.AddRange(request.CurrentExecutionInfo.CurrentSpec.Tags.Select(t => new ItemAttributeConverter().ConvertFrom(t, opts => opts.UndefinedKey = "tag")).ToList());

            var startTestItemRequest = new StartTestItemRequest
            {
                Type = TestItemType.Step,
                StartTime = DateTime.UtcNow,
                Name = scenario.ScenarioHeading,
                Description = string.Join("", scenario.ScenarioItems.Where(i => i.ItemType == ProtoItem.Types.ItemType.Comment).Select(c => c.Comment.Text)),
                Attributes = attributes,
                TestCaseId = testCaseIdTagValue,
                Parameters = scenarioRequestParameters
            };

            // parse scenario retry
            if (request.CurrentExecutionInfo?.ExecutionArgs?.Any(arg => arg.FlagName.Equals("max-retries-count", StringComparison.InvariantCultureIgnoreCase)) == true)
            {
                startTestItemRequest.IsRetry = true;
            }

            var scenarioReporter = specReporter.StartChildTestReporter(startTestItemRequest);

            // pre hook messages
            if (scenarioResult.Scenario.PreHookMessages.Count != 0 || scenarioResult.Scenario.PreHookFailure != null)
            {
                foreach (var preHookMessage in scenarioResult.Scenario.PreHookMessages)
                {
                    scenarioReporter.Log(new CreateLogItemRequest
                    {
                        Level = LogLevel.Debug,
                        Text = preHookMessage,
                        Time = DateTime.UtcNow
                    });
                }

                if (scenarioResult.Scenario.PreHookFailure != null)
                {
                    var preHookFailure = scenarioResult.Scenario.PreHookFailure;

                    scenarioReporter.Log(new CreateLogItemRequest
                    {
                        Level = LogLevel.Error,
                        Text = $"{preHookFailure.ErrorMessage}{Environment.NewLine}{preHookFailure.StackTrace}",
                        Time = DateTime.UtcNow
                    });
                }
            }

            var key = GetScenarioKey(request.CurrentExecutionInfo, request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario);
            _scenarios[key] = scenarioReporter;
        }

        public void FinishScenario(ScenarioExecutionEndingRequest request)
        {
            var key = GetScenarioKey(request.CurrentExecutionInfo, request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario);

            var scenarioReporter = _scenarios[key];

            var scenarioResult = request.ScenarioResult;

            // post hook messages
            if (scenarioResult.ProtoItem.Scenario.PostHookMessages.Count != 0 || scenarioResult.ProtoItem.Scenario.PostHookFailure != null)
            {
                foreach (var postHookMessage in scenarioResult.ProtoItem.Scenario.PostHookMessages)
                {
                    scenarioReporter.Log(new CreateLogItemRequest
                    {
                        Level = LogLevel.Debug,
                        Text = postHookMessage,
                        Time = DateTime.UtcNow
                    });
                }

                if (scenarioResult.ProtoItem.Scenario.PostHookFailure != null)
                {
                    var postHookFailure = scenarioResult.ProtoItem.Scenario.PostHookFailure;

                    scenarioReporter.Log(new CreateLogItemRequest
                    {
                        Level = LogLevel.Error,
                        Text = $"{postHookFailure.ErrorMessage}{Environment.NewLine}{postHookFailure.StackTrace}",
                        Time = DateTime.UtcNow
                    });
                }
            }

            scenarioReporter.Finish(new FinishTestItemRequest
            {
                EndTime = DateTime.UtcNow,
                Status = _statusMap[scenarioResult.ProtoItem.Scenario.ExecutionStatus]
            });

            _scenarios.TryRemove(key, out _);
        }

        private string GetScenarioKey(ExecutionInfo executionInfo, SpecInfo specInfo, ScenarioInfo scenarioInfo)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { Spec = GetSpecKey(executionInfo, specInfo), ScenarioName = scenarioInfo.Name });
        }
    }
}
