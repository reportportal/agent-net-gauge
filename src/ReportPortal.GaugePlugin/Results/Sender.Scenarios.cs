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

            var scenarioReporter = specReporter.StartChildTestReporter(new StartTestItemRequest
            {
                Type = TestItemType.Step,
                StartTime = DateTime.UtcNow,
                Name = scenario.ScenarioHeading,
                Description = string.Join("", scenario.ScenarioItems.Where(i => i.ItemType == ProtoItem.Types.ItemType.Comment).Select(c => c.Comment.Text)),
                Attributes = scenario.Tags.Select(t => new ItemAttribute { Value = t.ToString() }).ToList(),
                TestCaseId = testCaseIdTagValue
            });

            var key = GetScenarioKey(request.CurrentExecutionInfo, request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario);
            _scenarios[key] = scenarioReporter;
        }

        public void FinishScenario(ScenarioExecutionEndingRequest request)
        {
            var key = GetScenarioKey(request.CurrentExecutionInfo, request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario);

            _scenarios[key].Finish(new FinishTestItemRequest
            {
                EndTime = DateTime.UtcNow,
                Status = _statusMap[request.ScenarioResult.ProtoItem.Scenario.ExecutionStatus]
            });

            _scenarios.TryRemove(key, out _);
        }

        private string GetScenarioKey(ExecutionInfo executionInfo, SpecInfo specInfo, ScenarioInfo scenarioInfo)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { Spec = GetSpecKey(executionInfo, specInfo), ScenarioName = scenarioInfo.Name });
        }
    }
}
