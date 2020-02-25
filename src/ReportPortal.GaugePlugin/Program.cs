using Gauge.Messages;
using Grpc.Core;
using ReportPortal.Client;
using ReportPortal.GaugePlugin.Results;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Configuration.Providers;
using ReportPortal.Shared.Internal.Logging;
using System;
using System.Threading.Tasks;

namespace ReportPortal.GaugePlugin
{
    class Program
    {
        private static ITraceLogger TraceLogger = TraceLogManager.GetLogger<Program>();

        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .Add(new EnvironmentVariablesConfigurationProvider("RP_", "_", EnvironmentVariableTarget.Process))
                .Build();

            var url = configuration.GetValue<string>(ConfigurationPath.ServerUrl);
            var project = configuration.GetValue<string>(ConfigurationPath.ServerProject);
            var apiToken = configuration.GetValue<string>(ConfigurationPath.ServerAuthenticationUuid);
            var apiClientService = new Service(new Uri(url), project, apiToken);

            var sender = new Sender(apiClientService, configuration);

            var server = new Server();

            var s = Reporter.BindService(new ReportMessagesHandler(server, sender));
            server.Services.Add(s);

            var g_port = server.Ports.Add(new ServerPort("localhost", 0, ServerCredentials.Insecure));
            server.Start();

            Console.Write($"Listening on port:{g_port}");

            TraceLogger.Info("Server has started.");

            await server.ShutdownTask;






            //var rpUri = new Uri(Config.GetValue<string>("Uri"));
            //var rpProject = Config.GetValue<string>("Project");
            //var rpUuid = Config.GetValue<string>("Uuid");

            //var service = new Service(rpUri, rpProject, rpUuid);
            //var launchReporter = new LaunchReporter(service, Config, null);

            //var tcpClientWrapper = new TcpClientWrapper(Gauge.CSharp.Core.Utils.GaugeApiPort);
            //using (var gaugeConnection = new GaugeConnection(tcpClientWrapper))
            //{
            //    while (gaugeConnection.Connected)
            //    {
            //        try
            //        {
            //            TraceLogger.Verbose($"Reading message...");

            //            var messageBytes = gaugeConnection.ReadBytes();
            //            TraceLogger.Verbose($"Read message. Length: {messageBytes.Count()}");

            //            var message = Message.Parser.ParseFrom(messageBytes.ToArray());
            //            TraceLogger.Verbose($"Received event {message.MessageType}");

            //            //if (message.MessageType == Message.Types.MessageType.SuiteExecutionResult)
            //            //{
            //            //    var suiteExecutionResult = message.SuiteExecutionResult.SuiteResult;

            //            //    var launchStartDateTime = DateTime.UtcNow.AddMilliseconds(-suiteExecutionResult.ExecutionTime);
            //            //    launchReporter.Start(new Client.Requests.StartLaunchRequest
            //            //    {
            //            //        Name = Config.GetValue("Launch:Name", suiteExecutionResult.ProjectName),
            //            //        Description = Config.GetValue("Launch:Description", string.Empty),
            //            //        Tags = Config.GetValues("Launch:Tags", new List<string>()).ToList(),
            //            //        StartTime = launchStartDateTime
            //            //    });

            //            //    foreach (var specResult in suiteExecutionResult.SpecResults)
            //            //    {
            //            //        var specStartTime = launchStartDateTime;
            //            //        var specReporter = launchReporter.StartChildTestReporter(new Client.Requests.StartTestItemRequest
            //            //        {
            //            //            Type = Client.Models.TestItemType.Suite,
            //            //            Name = specResult.ProtoSpec.SpecHeading,
            //            //            Description = string.Join("", specResult.ProtoSpec.Items.Where(i => i.ItemType == ProtoItem.Types.ItemType.Comment).Select(c => c.Comment.Text)),
            //            //            StartTime = specStartTime,
            //            //            Tags = specResult.ProtoSpec.Tags.Select(t => t.ToString()).ToList()
            //            //        });

            //            //        foreach (var scenarioResult in specResult.ProtoSpec.Items.Where(i => i.ItemType == ProtoItem.Types.ItemType.Scenario || i.ItemType == ProtoItem.Types.ItemType.TableDrivenScenario))
            //            //        {
            //            //            ProtoScenario scenario;

            //            //            switch (scenarioResult.ItemType)
            //            //            {
            //            //                case ProtoItem.Types.ItemType.Scenario:
            //            //                    scenario = scenarioResult.Scenario;
            //            //                    break;
            //            //                case ProtoItem.Types.ItemType.TableDrivenScenario:
            //            //                    scenario = scenarioResult.TableDrivenScenario.Scenario;
            //            //                    break;
            //            //                default:
            //            //                    scenario = scenarioResult.Scenario;
            //            //                    break;
            //            //            }

            //            //            var scenarioStartTime = specStartTime;
            //            //            var scenarioReporter = specReporter.StartChildTestReporter(new Client.Requests.StartTestItemRequest
            //            //            {
            //            //                Type = Client.Models.TestItemType.Step,
            //            //                StartTime = scenarioStartTime,
            //            //                Name = scenario.ScenarioHeading,
            //            //                Description = string.Join("", scenario.ScenarioItems.Where(i => i.ItemType == ProtoItem.Types.ItemType.Comment).Select(c => c.Comment.Text)),
            //            //                Tags = scenario.Tags.Select(t => t.ToString()).ToList()
            //            //            });

            //            //            // internal log ("rp_log_enabled" property)
            //            //            if (Config.GetValue("log:enabled", false))
            //            //            {
            //            //                scenarioReporter.Log(new Client.Requests.AddLogItemRequest
            //            //                {
            //            //                    Text = "Spec Result Proto",
            //            //                    Level = Client.Models.LogLevel.Trace,
            //            //                    Time = DateTime.UtcNow,
            //            //                    Attach = new Client.Models.Attach("Spec", "application/json", System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(specResult)))
            //            //                });
            //            //                scenarioReporter.Log(new Client.Requests.AddLogItemRequest
            //            //                {
            //            //                    Text = "Scenario Result Proto",
            //            //                    Level = Client.Models.LogLevel.Trace,
            //            //                    Time = DateTime.UtcNow,
            //            //                    Attach = new Client.Models.Attach("Scenario", "application/json", System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(scenarioResult)))
            //            //                });
            //            //            }

            //            //            var lastStepStartTime = scenarioStartTime;
            //            //            if (scenario.ScenarioItems != null)
            //            //            {
            //            //                foreach (var stepResult in scenario.ScenarioItems.Where(i => i.ItemType == ProtoItem.Types.ItemType.Step))
            //            //                {
            //            //                    var text = "!!!MARKDOWN_MODE!!!" + stepResult.Step.ActualText;
            //            //                    var stepLogLevel = stepResult.Step.StepExecutionResult.ExecutionResult.Failed ? Client.Models.LogLevel.Error : Client.Models.LogLevel.Info;

            //            //                    // if step argument is table
            //            //                    var tableParameter = stepResult.Step.Fragments.FirstOrDefault(f => f.Parameter?.Table != null)?.Parameter.Table;
            //            //                    if (tableParameter != null)
            //            //                    {
            //            //                        text += Environment.NewLine + Environment.NewLine + "| " + string.Join(" | ", tableParameter.Headers.Cells.ToArray()) + " |";
            //            //                        text += Environment.NewLine + "| " + string.Join(" | ", tableParameter.Headers.Cells.Select(c => "---")) + " |";

            //            //                        foreach (var tableRow in tableParameter.Rows)
            //            //                        {
            //            //                            text += Environment.NewLine + "| " + string.Join(" | ", tableRow.Cells.ToArray()) + " |";
            //            //                        }
            //            //                    }

            //            //                    // if dynamic arguments
            //            //                    var dynamicParameteres = stepResult.Step.Fragments.Where(f => f.FragmentType == Fragment.Types.FragmentType.Parameter && f.Parameter.ParameterType == Parameter.Types.ParameterType.Dynamic).Select(f => f.Parameter);
            //            //                    if (dynamicParameteres.Count() != 0)
            //            //                    {
            //            //                        text += Environment.NewLine;

            //            //                        foreach (var dynamicParameter in dynamicParameteres)
            //            //                        {
            //            //                            text += $"{Environment.NewLine}{dynamicParameter.Name}: {dynamicParameter.Value}";
            //            //                        }
            //            //                    }

            //            //                    if (stepResult.Step.StepExecutionResult.ExecutionResult.Failed)
            //            //                    {
            //            //                        text += $"{Environment.NewLine}{Environment.NewLine}{stepResult.Step.StepExecutionResult.ExecutionResult.ErrorMessage}{Environment.NewLine}{stepResult.Step.StepExecutionResult.ExecutionResult.StackTrace}";
            //            //                    }

            //            //                    scenarioReporter.Log(new Client.Requests.AddLogItemRequest
            //            //                    {
            //            //                        Level = stepLogLevel,
            //            //                        Time = lastStepStartTime,
            //            //                        Text = text
            //            //                    });

            //            //                    if (stepResult.Step.StepExecutionResult.ExecutionResult.ScreenShot?.Length != 0)
            //            //                    {
            //            //                        scenarioReporter.Log(new Client.Requests.AddLogItemRequest
            //            //                        {
            //            //                            Level = Client.Models.LogLevel.Debug,
            //            //                            Time = lastStepStartTime,
            //            //                            Text = "Screenshot",
            //            //                            Attach = new Client.Models.Attach("Screenshot", "image/png", stepResult.Step.StepExecutionResult.ExecutionResult.ScreenShot.ToByteArray())
            //            //                        });
            //            //                    }

            //            //                    lastStepStartTime = lastStepStartTime.AddMilliseconds(stepResult.Step.StepExecutionResult.ExecutionResult.ExecutionTime);
            //            //                }
            //            //            }

            //            //            scenarioReporter.Finish(new Client.Requests.FinishTestItemRequest
            //            //            {
            //            //                EndTime = scenarioStartTime.AddMilliseconds(scenario.ExecutionTime),
            //            //                Status = _statusMap[scenario.ExecutionStatus]
            //            //            });
            //            //        }

            //            //        var specFinishStatus = specResult.Failed ? Client.Models.Status.Failed : Client.Models.Status.Passed;
            //            //        specReporter.Finish(new Client.Requests.FinishTestItemRequest
            //            //        {
            //            //            Status = specFinishStatus,
            //            //            EndTime = specStartTime.AddMilliseconds(specResult.ExecutionTime)
            //            //        });
            //            //    }

            //            //    launchReporter.Finish(new Client.Requests.FinishLaunchRequest
            //            //    {
            //            //        EndTime = DateTime.UtcNow
            //            //    });
            //            //}

            //            if (message.MessageType == Message.Types.MessageType.KillProcessRequest)
            //            {
            //                Console.Write("Finishing to send results to Report Portal... ");
            //                var sw = Stopwatch.StartNew();
            //                launchReporter.Sync();

            //                Console.WriteLine($"Elapsed: {sw.Elapsed}");

            //                return;
            //            }
            //        }
            //        catch (Exception exp)
            //        {
            //            TraceLogger.Error($"Unhandler error: {exp}");
            //        }
            //    }
            //}


        }
    }
}
