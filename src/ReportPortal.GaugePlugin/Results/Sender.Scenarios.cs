using Gauge.Messages;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Client.Abstractions.Responses;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportPortal.GaugePlugin.Results
{
    partial class Sender
    {
        private Dictionary<string, ITestReporter> _scenarios = new Dictionary<string, ITestReporter>();

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

            var specReporter = _specs[GetSpecKey(request.CurrentExecutionInfo.CurrentSpec)];

            var scenarioReporter = specReporter.StartChildTestReporter(new StartTestItemRequest
            {
                Type = TestItemType.Step,
                StartTime = DateTime.UtcNow,
                Name = scenario.ScenarioHeading,
                Description = string.Join("", scenario.ScenarioItems.Where(i => i.ItemType == ProtoItem.Types.ItemType.Comment).Select(c => c.Comment.Text)),
                Tags = scenario.Tags.Select(t => t.ToString()).ToList()
            });

            var key = GetScenarioKey(request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario);
            _scenarios[key] = scenarioReporter;
        }

        public void FinishScenario(ScenarioExecutionEndingRequest request)
        {
            var key = GetScenarioKey(request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario);

            _scenarios[key].Finish(new FinishTestItemRequest
            {
                EndTime = DateTime.UtcNow,
                Status = _statusMap[request.ScenarioResult.ProtoItem.Scenario.ExecutionStatus]
            });

            _scenarios.Remove(key);
        }

        private string GetScenarioKey(SpecInfo specInfo, ScenarioInfo scenarioInfo)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { specInfo.FileName, specInfo.Name, ScenarioName = scenarioInfo.Name });
        }
    }
}
