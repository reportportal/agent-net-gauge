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
        private Dictionary<string, ITestReporter> _steps = new Dictionary<string, ITestReporter>();

        public void StartStep(StepExecutionStartingRequest request)
        {
            var stepResult = request.StepResult;

            var scenarioReporter = _scenarios[GetScenarioKey(request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario)];

            var stepReporter = scenarioReporter.StartChildTestReporter(new StartTestItemRequest
            {
                Type = TestItemType.Step,
                StartTime = DateTime.UtcNow,
                Name = stepResult.ProtoItem.Step.ActualText,
                HasStats = false
            });

            #region step table
            //if step argument is table
            var tableParameter = stepResult.ProtoItem.Step.Fragments.FirstOrDefault(f => f.Parameter?.Table != null)?.Parameter.Table;
            if (tableParameter != null)
            {
                var text = "| **" + string.Join("** | **", tableParameter.Headers.Cells.ToArray()) + "** |";
                text += Environment.NewLine + "| " + string.Join(" | ", tableParameter.Headers.Cells.Select(c => "---")) + " |";

                foreach (var tableRow in tableParameter.Rows)
                {
                    text += Environment.NewLine + "| " + string.Join(" | ", tableRow.Cells.ToArray()) + " |";
                }

                stepReporter.Log(new CreateLogItemRequest
                {
                    Time = DateTime.UtcNow,
                    Level = LogLevel.Info,
                    Text = text
                });
            }

            // if dynamic arguments
            var dynamicParameteres = stepResult.ProtoItem.Step.Fragments.Where(f => f.FragmentType == Fragment.Types.FragmentType.Parameter && f.Parameter.ParameterType == Parameter.Types.ParameterType.Dynamic).Select(f => f.Parameter);
            if (dynamicParameteres.Count() != 0)
            {
                var text = "";

                foreach (var dynamicParameter in dynamicParameteres)
                {
                    text += $"{Environment.NewLine}{dynamicParameter.Name}: {dynamicParameter.Value}";
                }
            }
            #endregion

            var key = GetStepKey(request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario, request.CurrentExecutionInfo.CurrentStep);
            _steps[key] = stepReporter;
        }

        public void FinishStep(StepExecutionEndingRequest request)
        {
            var key = GetStepKey(request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario, request.CurrentExecutionInfo.CurrentStep);

            var stepReporter = _steps[key];

            var stepStatus = Status.Passed;

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

            stepReporter.Finish(new FinishTestItemRequest
            {
                EndTime = DateTime.UtcNow,
                Status = stepStatus
            });

            _steps.Remove(key);
        }

        private string GetStepKey(SpecInfo specInfo, ScenarioInfo scenarioInfo, StepInfo stepInfo)
        {
            return System.Text.Json.JsonSerializer.Serialize(new { specInfo.FileName, specInfo.Name, ScenarioName = scenarioInfo.Name, StepName = stepInfo.Step.ActualStepText });
        }
    }
}
