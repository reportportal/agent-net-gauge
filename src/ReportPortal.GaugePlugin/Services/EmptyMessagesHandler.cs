using Gauge.Messages;
using Grpc.Core;
using ReportPortal.Shared.Internal.Logging;
using System.Threading.Tasks;

namespace ReportPortal.GaugePlugin.Services
{
    class EmptyMessagesHandler : Reporter.ReporterBase
    {
        private static ITraceLogger TraceLogger = TraceLogManager.Instance.GetLogger<EmptyMessagesHandler>();

        public override Task<Empty> NotifyExecutionStarting(ExecutionStartingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyExecutionEnding(ExecutionEndingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifySpecExecutionStarting(SpecExecutionStartingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifySpecExecutionEnding(SpecExecutionEndingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyScenarioExecutionStarting(ScenarioExecutionStartingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyScenarioExecutionEnding(ScenarioExecutionEndingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyConceptExecutionStarting(ConceptExecutionStartingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyConceptExecutionEnding(ConceptExecutionEndingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyStepExecutionStarting(StepExecutionStartingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyStepExecutionEnding(StepExecutionEndingRequest request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifySuiteResult(SuiteExecutionResult request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> Kill(KillProcessRequest request, ServerCallContext context)
        {
            TraceLogger.Info("Kill received");

            try
            {
                return Task.FromResult(new Empty());
            }
            finally
            {
                Program.ShutDownCancellationSource.Cancel();
            }
        }
    }
}
