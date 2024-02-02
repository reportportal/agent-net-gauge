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
        /// <summary>
        /// Assuming scenario might have only on running concept.
        /// </summary>
        private readonly ConcurrentDictionary<ScenarioKey, List<ITestReporter>> _scenarioConcepts = new();

        public void StartConcept(ConceptExecutionStartingRequest request)
        {
            var scenarioKey = GetScenarioKey(request.CurrentExecutionInfo, request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario);

            var parentReporter = _scenarioConcepts.ContainsKey(scenarioKey) ? _scenarioConcepts[scenarioKey].Last() : _scenarios[scenarioKey];

            var conceptReporter = parentReporter.StartChildTestReporter(new StartTestItemRequest
            {
                Name = request.CurrentExecutionInfo.CurrentStep.Step.ActualStepText,
                StartTime = DateTime.UtcNow,
                HasStats = false
            });

            if (_scenarioConcepts.TryGetValue(scenarioKey, out List<ITestReporter> concepts))
            {
                concepts.Add(conceptReporter);
            }
            else
            {
                _scenarioConcepts[scenarioKey] = [conceptReporter];
            }
        }

        public void FinishConcept(ConceptExecutionEndingRequest request)
        {
            var scenarioKey = GetScenarioKey(request.CurrentExecutionInfo, request.CurrentExecutionInfo.CurrentSpec, request.CurrentExecutionInfo.CurrentScenario);

            var conceptReporter = _scenarioConcepts[scenarioKey].Last();

            var status = request.CurrentExecutionInfo.CurrentScenario.IsFailed ? Status.Failed : Status.Passed;

            conceptReporter.Finish(new FinishTestItemRequest
            {
                Status = status,
                EndTime = DateTime.UtcNow
            });

            var concepts = _scenarioConcepts[scenarioKey];

            if (concepts.Count == 1)
            {
                _scenarioConcepts.Remove(scenarioKey, out _);
            }
            else
            {
                concepts.Remove(conceptReporter);
            }
        }
    }
}
