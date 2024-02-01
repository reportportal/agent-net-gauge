using Gauge.Messages;
using Grpc.Core;
using ReportPortal.GaugePlugin.Results;
using ReportPortal.Shared.Internal.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ReportPortal.GaugePlugin.Services
{
    class ReportMessagesHandler : Reporter.ReporterBase
    {
        private static readonly ITraceLogger TraceLogger = TraceLogManager.Instance.GetLogger<ReportMessagesHandler>();

        private readonly Sender _sender;

        public ReportMessagesHandler(Sender sender)
        {
            _sender = sender;
        }

        public override Task<Empty> NotifyExecutionStarting(ExecutionStartingRequest request, ServerCallContext context)
        {
            try
            {
                TraceLogger.Info($"{nameof(NotifyExecutionStarting)} received");
                TraceLogger.Verbose(request.ToString());

                if (request.SuiteResult != null)
                {
                    _sender.StartLaunch(request);
                }
            }
            catch (Exception exp)
            {
                TraceLogger.Error(exp.ToString());
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyExecutionEnding(ExecutionEndingRequest request, ServerCallContext context)
        {
            try
            {
                TraceLogger.Info($"{nameof(NotifyExecutionEnding)} received");
                TraceLogger.Verbose(request.ToString());

                if (request.SuiteResult != null)
                {
                    _sender.FinishLaunch(request);
                }
            }
            catch (Exception exp)
            {
                TraceLogger.Error(exp.ToString());
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifySpecExecutionStarting(SpecExecutionStartingRequest request, ServerCallContext context)
        {
            try
            {
                TraceLogger.Info($"{nameof(NotifySpecExecutionStarting)} received");
                TraceLogger.Verbose(request.ToString());

                if (request.SpecResult != null)
                {
                    _sender.StartSpec(request);
                }
            }
            catch (Exception exp)
            {
                TraceLogger.Error(exp.ToString());
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifySpecExecutionEnding(SpecExecutionEndingRequest request, ServerCallContext context)
        {
            try
            {
                TraceLogger.Info($"{nameof(NotifySpecExecutionEnding)} received");
                TraceLogger.Verbose(request.ToString());

                if (request.SpecResult != null)
                {
                    _sender.FinishSpec(request);
                }
            }
            catch (Exception exp)
            {
                TraceLogger.Error(exp.ToString());
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyScenarioExecutionStarting(ScenarioExecutionStartingRequest request, ServerCallContext context)
        {
            try
            {
                TraceLogger.Info($"{nameof(NotifyScenarioExecutionStarting)} received");
                TraceLogger.Verbose(request.ToString());

                if (request.ScenarioResult != null)
                {
                    _sender.StartScenario(request);
                }
            }
            catch (Exception exp)
            {
                TraceLogger.Error(exp.ToString());
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyScenarioExecutionEnding(ScenarioExecutionEndingRequest request, ServerCallContext context)
        {
            try
            {
                TraceLogger.Info($"{nameof(NotifyScenarioExecutionEnding)} received");
                TraceLogger.Verbose(request.ToString());

                if (request.ScenarioResult != null)
                {
                    _sender.FinishScenario(request);
                }
            }
            catch (Exception exp)
            {
                TraceLogger.Error(exp.ToString());
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyConceptExecutionStarting(ConceptExecutionStartingRequest request, ServerCallContext context)
        {
            try
            {
                TraceLogger.Info($"{nameof(NotifyConceptExecutionStarting)} received");
                TraceLogger.Verbose(request.ToString());

                // do nothing for now
            }
            catch (Exception exp)
            {
                TraceLogger.Error(exp.ToString());
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyConceptExecutionEnding(ConceptExecutionEndingRequest request, ServerCallContext context)
        {
            try
            {
                TraceLogger.Info($"{nameof(NotifyConceptExecutionEnding)} received");
                TraceLogger.Verbose(request.ToString());

                // do nothing for now
            }
            catch (Exception exp)
            {
                TraceLogger.Error(exp.ToString());
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyStepExecutionStarting(StepExecutionStartingRequest request, ServerCallContext context)
        {
            try
            {
                TraceLogger.Info($"{nameof(NotifyStepExecutionStarting)} received");
                TraceLogger.Verbose(request.ToString());

                if (request.StepResult != null)
                {
                    _sender.StartStep(request);
                }
            }
            catch (Exception exp)
            {
                TraceLogger.Error(exp.ToString());
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyStepExecutionEnding(StepExecutionEndingRequest request, ServerCallContext context)
        {
            try
            {
                TraceLogger.Info($"{nameof(NotifyStepExecutionEnding)} received");
                TraceLogger.Verbose(request.ToString());

                if (request.StepResult != null)
                {
                    _sender.FinishStep(request);
                }
            }
            catch (Exception exp)
            {
                TraceLogger.Error(exp.ToString());
            }

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
                try
                {
                    Console.Write("Finishing to send results to Report Portal... ");
                    var sw = Stopwatch.StartNew();

                    _sender.Sync();

                    Console.WriteLine($"Successfully sent at {_sender.LaunchReporter.Info.Url} Elapsed: {sw.Elapsed}");
                }
                catch (Exception exp)
                {
                    Console.WriteLine($"Unexpected error: {exp}");
                }
                finally
                {
                    if (_sender != null)
                    {
                        var statsMessage = _sender.LaunchReporter.StatisticsCounter.ToString();

                        TraceLogger.Info(statsMessage);

                        Console.WriteLine(statsMessage);
                    }
                }

                return Task.FromResult(new Empty());
            }
            finally
            {
                Program.ShutDownCancelationSource.Cancel();
            }
        }
    }
}

