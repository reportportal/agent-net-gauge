using Gauge.Messages;
using Grpc.Core;
using ReportPortal.GaugePlugin.Results;
using ReportPortal.Shared.Internal.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ReportPortal.GaugePlugin
{
    class ReportMessagesHandler : Reporter.ReporterBase
    {
        private static ITraceLogger TraceLogger = TraceLogManager.Instance.GetLogger<ReportMessagesHandler>();

        private Server _server;

        private Sender _sender;

        private static readonly object lockObj = new object();

        public ReportMessagesHandler(Server server, Sender sender)
        {
            _server = server;

            _sender = sender;
        }

        public override Task<Empty> NotifyExecutionStarting(ExecutionStartingRequest request, ServerCallContext context)
        {
            lock (lockObj)
            {
                try
                {
                    TraceLogger.Info($"{nameof(NotifyExecutionStarting)} received");
                    TraceLogger.Verbose(System.Text.Json.JsonSerializer.Serialize(request));

                    if (request.SuiteResult != null)
                    {
                        _sender.StartLaunch(request);
                    }
                }
                catch (Exception exp)
                {
                    TraceLogger.Error(exp.ToString());
                }
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyExecutionEnding(ExecutionEndingRequest request, ServerCallContext context)
        {
            lock (lockObj)
            {
                try
                {
                    TraceLogger.Info($"{nameof(NotifyExecutionEnding)} received");
                    TraceLogger.Verbose(System.Text.Json.JsonSerializer.Serialize(request));

                    if (request.SuiteResult != null)
                    {
                        _sender.FinishLaunch(request);
                    }
                }
                catch (Exception exp)
                {
                    TraceLogger.Error(exp.ToString());
                }
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifySpecExecutionStarting(SpecExecutionStartingRequest request, ServerCallContext context)
        {
            lock (lockObj)
            {
                try
                {
                    TraceLogger.Info($"{nameof(NotifySpecExecutionStarting)} received");
                    TraceLogger.Verbose(System.Text.Json.JsonSerializer.Serialize(request));

                    if (request.SpecResult != null)
                    {
                        _sender.StartSpec(request);
                    }
                }
                catch (Exception exp)
                {
                    TraceLogger.Error(exp.ToString());
                }
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifySpecExecutionEnding(SpecExecutionEndingRequest request, ServerCallContext context)
        {
            lock (lockObj)
            {
                try
                {
                    TraceLogger.Info($"{nameof(NotifySpecExecutionEnding)} received");
                    TraceLogger.Verbose(System.Text.Json.JsonSerializer.Serialize(request));

                    if (request.SpecResult != null)
                    {
                        _sender.FinishSpec(request);
                    }
                }
                catch (Exception exp)
                {
                    TraceLogger.Error(exp.ToString());
                }
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyScenarioExecutionStarting(ScenarioExecutionStartingRequest request, ServerCallContext context)
        {
            lock (lockObj)
            {
                try
                {
                    TraceLogger.Info($"{nameof(NotifyScenarioExecutionStarting)} received");
                    TraceLogger.Verbose(System.Text.Json.JsonSerializer.Serialize(request));

                    if (request.ScenarioResult != null)
                    {
                        _sender.StartScenario(request);
                    }
                }
                catch (Exception exp)
                {
                    TraceLogger.Error(exp.ToString());
                }
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyScenarioExecutionEnding(ScenarioExecutionEndingRequest request, ServerCallContext context)
        {
            lock (lockObj)
            {
                try
                {
                    TraceLogger.Info($"{nameof(NotifyScenarioExecutionEnding)} received");
                    TraceLogger.Verbose(System.Text.Json.JsonSerializer.Serialize(request));

                    if (request.ScenarioResult != null)
                    {
                        _sender.FinishScenario(request);
                    }
                }
                catch (Exception exp)
                {
                    TraceLogger.Error(exp.ToString());
                }
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyStepExecutionStarting(StepExecutionStartingRequest request, ServerCallContext context)
        {
            lock (lockObj)
            {
                try
                {
                    TraceLogger.Info($"{nameof(NotifyStepExecutionStarting)} received");
                    TraceLogger.Verbose(System.Text.Json.JsonSerializer.Serialize(request));

                    if (request.StepResult != null)
                    {
                        _sender.StartStep(request);
                    }
                }
                catch (Exception exp)
                {
                    TraceLogger.Error(exp.ToString());
                }
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifyStepExecutionEnding(StepExecutionEndingRequest request, ServerCallContext context)
        {
            lock (lockObj)
            {
                try
                {
                    TraceLogger.Info($"{nameof(NotifyStepExecutionEnding)} received");
                    TraceLogger.Verbose(System.Text.Json.JsonSerializer.Serialize(request));

                    if (request.StepResult != null)
                    {
                        _sender.FinishStep(request);
                    }
                }
                catch (Exception exp)
                {
                    TraceLogger.Error(exp.ToString());
                }
            }

            return Task.FromResult(new Empty());
        }

        public override Task<Empty> NotifySuiteResult(SuiteExecutionResult request, ServerCallContext context)
        {
            return Task.FromResult(new Empty());
        }

        public override async Task<Empty> Kill(KillProcessRequest request, ServerCallContext context)
        {

            TraceLogger.Info("Kill received");
            try
            {
                try
                {
                    lock (lockObj)
                    {
                        Console.Write("Finishing to send results to Report Portal... ");
                        var sw = Stopwatch.StartNew();
                        _sender.Sync();
                        Console.WriteLine($"Successfully sent. Elapsed: {sw.Elapsed}");
                    }
                }
                catch (Exception exp)
                {
                    Console.WriteLine($"Unexpected errors: {exp}");
                }

                return new Empty();
            }
            finally
            {
                await _server.KillAsync();
            }
        }
    }
}

