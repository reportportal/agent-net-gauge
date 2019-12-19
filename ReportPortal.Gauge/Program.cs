using Gauge.CSharp.Core;
using Gauge.Messages;
using ReportPortal.Client;
using ReportPortal.Shared;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Configuration.Providers;
using ReportPortal.Shared.Internal.Delegating;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ReportPortal.Gauge
{
    class Program
    {
        private static Dictionary<ExecutionStatus, Client.Models.Status> _statusMap;

        private static IConfiguration Config;

        static Program()
        {
            _statusMap = new Dictionary<ExecutionStatus, Client.Models.Status>
            {
                { ExecutionStatus.Failed, Client.Models.Status.Failed },
                { ExecutionStatus.Notexecuted, Client.Models.Status.Skipped },
                { ExecutionStatus.Passed, Client.Models.Status.Passed },
                { ExecutionStatus.Skipped, Client.Models.Status.Skipped }
            };

            var configPrefix = "RP_";
            var configDelimeter = "_";
            Config = new ConfigurationBuilder()
                .Add(new EnvironmentVariablesConfigurationProvider(configPrefix, configDelimeter, EnvironmentVariableTarget.Process))
                .Add(new EnvironmentVariablesConfigurationProvider(configPrefix, configDelimeter, EnvironmentVariableTarget.User))
                .Add(new EnvironmentVariablesConfigurationProvider(configPrefix, configDelimeter, EnvironmentVariableTarget.Machine))
                .Build();
        }

        static void Main(string[] args)
        {
            var port = Convert.ToInt32(Environment.GetEnvironmentVariable("plugin_connection_port"));

            var rpUri = new Uri(Config.GetValue<string>("Uri"));
            var rpProject = Config.GetValue<string>("Project");
            var rpUuid = Config.GetValue<string>("Uuid");

            var service = new Service(rpUri, rpProject, rpUuid);
            var launchReporter = new LaunchReporter(service, Config, requestExecuterFactory: null);

            using (var gaugeConnection = new GaugeConnection(new TcpClientWrapper(port)))
            {
                while (gaugeConnection.Connected)
                {
                    var message = Message.Parser.ParseFrom(gaugeConnection.ReadBytes().ToArray());

                    if (message.MessageType == Message.Types.MessageType.SuiteExecutionResult)
                    {
                        var suiteExecutionResult = message.SuiteExecutionResult.SuiteResult;

                        var launchStartDateTime = DateTime.UtcNow.AddMilliseconds(-suiteExecutionResult.ExecutionTime);
                        launchReporter.Start(new Client.Requests.StartLaunchRequest
                        {
                            Name = Config.GetValue("Launch:Name", suiteExecutionResult.ProjectName),
                            Description = Config.GetValue("Launch:Description", string.Empty),
                            Tags = Config.GetValues("Launch:Tags", new List<string>()).ToList(),
                            StartTime = launchStartDateTime
                        });

                        foreach (var specResult in suiteExecutionResult.SpecResults)
                        {
                            var specStartTime = launchStartDateTime;
                            var specReporter = launchReporter.StartChildTestReporter(new Client.Requests.StartTestItemRequest
                            {
                                Type = Client.Models.TestItemType.Suite,
                                Name = specResult.ProtoSpec.SpecHeading,
                                Description = string.Join("", specResult.ProtoSpec.Items.Where(i => i.ItemType == ProtoItem.Types.ItemType.Comment).Select(c => c.Comment.Text)),
                                StartTime = specStartTime,
                                Tags = specResult.ProtoSpec.Tags.Select(t => t.ToString()).ToList()
                            });

                            foreach (var scenarioResult in specResult.ProtoSpec.Items.Where(i => i.ItemType == ProtoItem.Types.ItemType.Scenario || i.ItemType == ProtoItem.Types.ItemType.TableDrivenScenario))
                            {
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

                                var scenarioStartTime = specStartTime;
                                var scenarioReporter = specReporter.StartChildTestReporter(new Client.Requests.StartTestItemRequest
                                {
                                    Type = Client.Models.TestItemType.Step,
                                    StartTime = scenarioStartTime,
                                    Name = scenario.ScenarioHeading,
                                    Description = string.Join("", scenario.ScenarioItems.Where(i => i.ItemType == ProtoItem.Types.ItemType.Comment).Select(c => c.Comment.Text)),
                                    Tags = scenario.Tags.Select(t => t.ToString()).ToList()
                                });

                                // internal log ("rp_log_enabled" property)
                                if (Config.GetValue("log:enabled", false))
                                {
                                    scenarioReporter.Log(new Client.Requests.AddLogItemRequest
                                    {
                                        Text = "Spec Result Proto",
                                        Level = Client.Models.LogLevel.Trace,
                                        Time = DateTime.UtcNow,
                                        Attach = new Client.Models.Attach("Spec", "application/json", System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(specResult)))
                                    });
                                    scenarioReporter.Log(new Client.Requests.AddLogItemRequest
                                    {
                                        Text = "Scenario Result Proto",
                                        Level = Client.Models.LogLevel.Trace,
                                        Time = DateTime.UtcNow,
                                        Attach = new Client.Models.Attach("Scenario", "application/json", System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(scenarioResult)))
                                    });
                                }

                                var lastStepStartTime = scenarioStartTime;
                                if (scenario.ScenarioItems != null)
                                {
                                    foreach (var stepResult in scenario.ScenarioItems.Where(i => i.ItemType == ProtoItem.Types.ItemType.Step))
                                    {
                                        var text = "!!!MARKDOWN_MODE!!!" + stepResult.Step.ActualText;
                                        var stepLogLevel = stepResult.Step.StepExecutionResult.ExecutionResult.Failed ? Client.Models.LogLevel.Error : Client.Models.LogLevel.Info;

                                        // if step argument is table
                                        var tableParameter = stepResult.Step.Fragments.FirstOrDefault(f => f.Parameter?.Table != null)?.Parameter.Table;
                                        if (tableParameter != null)
                                        {
                                            text += Environment.NewLine + Environment.NewLine + "| " + string.Join(" | ", tableParameter.Headers.Cells.ToArray()) + " |";
                                            text += Environment.NewLine + "| " + string.Join(" | ", tableParameter.Headers.Cells.Select(c => "---")) + " |";

                                            foreach (var tableRow in tableParameter.Rows)
                                            {
                                                text += Environment.NewLine + "| " + string.Join(" | ", tableRow.Cells.ToArray()) + " |";
                                            }
                                        }

                                        // if dynamic arguments
                                        var dynamicParameteres = stepResult.Step.Fragments.Where(f => f.FragmentType == Fragment.Types.FragmentType.Parameter && f.Parameter.ParameterType == Parameter.Types.ParameterType.Dynamic).Select(f => f.Parameter);
                                        if (dynamicParameteres.Count() != 0)
                                        {
                                            text += Environment.NewLine;

                                            foreach (var dynamicParameter in dynamicParameteres)
                                            {
                                                text += $"{Environment.NewLine}{dynamicParameter.Name}: {dynamicParameter.Value}";
                                            }
                                        }

                                        if (stepResult.Step.StepExecutionResult.ExecutionResult.Failed)
                                        {
                                            text += $"{Environment.NewLine}{Environment.NewLine}{stepResult.Step.StepExecutionResult.ExecutionResult.ErrorMessage}{Environment.NewLine}{stepResult.Step.StepExecutionResult.ExecutionResult.StackTrace}";
                                        }

                                        scenarioReporter.Log(new Client.Requests.AddLogItemRequest
                                        {
                                            Level = stepLogLevel,
                                            Time = lastStepStartTime,
                                            Text = text
                                        });

                                        if (stepResult.Step.StepExecutionResult.ExecutionResult.ScreenShot?.Length != 0)
                                        {
                                            scenarioReporter.Log(new Client.Requests.AddLogItemRequest
                                            {
                                                Level = Client.Models.LogLevel.Debug,
                                                Time = lastStepStartTime,
                                                Text = "Screenshot",
                                                Attach = new Client.Models.Attach("Screenshot", "image/png", stepResult.Step.StepExecutionResult.ExecutionResult.ScreenShot.ToByteArray())
                                            });
                                        }

                                        lastStepStartTime = lastStepStartTime.AddMilliseconds(stepResult.Step.StepExecutionResult.ExecutionResult.ExecutionTime);
                                    }
                                }

                                scenarioReporter.Finish(new Client.Requests.FinishTestItemRequest
                                {
                                    EndTime = scenarioStartTime.AddMilliseconds(scenario.ExecutionTime),
                                    Status = _statusMap[scenario.ExecutionStatus]
                                });
                            }

                            var specFinishStatus = specResult.Failed ? Client.Models.Status.Failed : Client.Models.Status.Passed;
                            specReporter.Finish(new Client.Requests.FinishTestItemRequest
                            {
                                Status = specFinishStatus,
                                EndTime = specStartTime.AddMilliseconds(specResult.ExecutionTime)
                            });
                        }

                        launchReporter.Finish(new Client.Requests.FinishLaunchRequest
                        {
                            EndTime = DateTime.UtcNow
                        });
                    }

                    if (message.MessageType == Message.Types.MessageType.KillProcessRequest)
                    {
                        Console.Write("Finishing to send results to Report Portal... ");
                        var sw = Stopwatch.StartNew();
                        launchReporter.Sync();

                        Console.WriteLine($"Elapsed: {sw.Elapsed}");

                        return;
                    }
                }
            }
        }

        static string ReadEnvVariable(string name)
        {
            var variable = Environment.GetEnvironmentVariable(name);

            if (string.IsNullOrEmpty(variable))
            {
                throw new Exception($"'{name}' variable is not defined in env file.");
            }

            return variable;
        }
    }
}
