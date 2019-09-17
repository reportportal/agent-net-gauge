using Gauge.CSharp.Core;
using Gauge.Messages;
using ReportPortal.Client;
using ReportPortal.Shared;
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

        static Program()
        {
            _statusMap = new Dictionary<ExecutionStatus, Client.Models.Status>
            {
                { ExecutionStatus.Failed, Client.Models.Status.Failed },
                { ExecutionStatus.Notexecuted, Client.Models.Status.Skipped },
                { ExecutionStatus.Passed, Client.Models.Status.Passed },
                { ExecutionStatus.Skipped, Client.Models.Status.Skipped }
            };
        }

        static void Main(string[] args)
        {
            var port = Convert.ToInt32(Environment.GetEnvironmentVariable("plugin_connection_port"));

            var rpUri = new Uri(ReadEnvVariable("rp_uri"));
            var rpProject = ReadEnvVariable("rp_project");
            var rpUuid = ReadEnvVariable("rp_uuid");

            var service = new Service(rpUri, rpProject, rpUuid);
            var launchReporter = new LaunchReporter(service);

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
                            Name = suiteExecutionResult.ProjectName,
                            Tags = suiteExecutionResult.Tags.Select(t => t.ToString()).ToList(),
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

                            foreach (var scenarioResult in specResult.ProtoSpec.Items.Where(i => i.ItemType == ProtoItem.Types.ItemType.Scenario))
                            {
                                var scenarioStartTime = specStartTime;
                                var scenarioReporter = specReporter.StartChildTestReporter(new Client.Requests.StartTestItemRequest
                                {
                                    Type = Client.Models.TestItemType.Step,
                                    StartTime = scenarioStartTime,
                                    Name = scenarioResult.Scenario.ScenarioHeading,
                                    Description = string.Join("", scenarioResult.Scenario.ScenarioItems.Where(i => i.ItemType == ProtoItem.Types.ItemType.Comment).Select(c => c.Comment.Text)),
                                    Tags = scenarioResult.Scenario.Tags.Select(t => t.ToString()).ToList()
                                });

                                // internal log
                                scenarioReporter.Log(new Client.Requests.AddLogItemRequest
                                {
                                    Text = "Spec Result Proto",
                                    Level = Client.Models.LogLevel.Trace,
                                    Time = DateTime.UtcNow,
                                    Attach = new Client.Models.Attach("Spec", "application/json", System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(specResult)))
                                });
                                // internal log
                                scenarioReporter.Log(new Client.Requests.AddLogItemRequest
                                {
                                    Text = "Scenario Result Proto",
                                    Level = Client.Models.LogLevel.Trace,
                                    Time = DateTime.UtcNow,
                                    Attach = new Client.Models.Attach("Scenario", "application/json", System.Text.Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(scenarioResult)))
                                });


                                foreach (var scenarioContext in scenarioResult.Scenario.Contexts)
                                {
                                    scenarioReporter.Log(new Client.Requests.AddLogItemRequest
                                    {
                                        Level = Client.Models.LogLevel.Info,
                                        Time = DateTime.UtcNow,
                                        Text = scenarioContext.Step.ActualText
                                    });
                                }

                                foreach (var stepResult in scenarioResult.Scenario.ScenarioItems.Where(i => i.ItemType == ProtoItem.Types.ItemType.Step))
                                {
                                    var text = stepResult.Step.ActualText;

                                    scenarioReporter.Log(new Client.Requests.AddLogItemRequest
                                    {
                                        Level = Client.Models.LogLevel.Info,
                                        Time = DateTime.UtcNow,
                                        Text = text
                                    });
                                }

                                scenarioReporter.Finish(new Client.Requests.FinishTestItemRequest
                                {
                                    EndTime = scenarioStartTime.AddMilliseconds(scenarioResult.Scenario.ExecutionTime),
                                    Status = _statusMap[scenarioResult.Scenario.ExecutionStatus]
                                });
                            }

                            specReporter.Finish(new Client.Requests.FinishTestItemRequest
                            {
                                Status = Client.Models.Status.Passed,
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
                        launchReporter.FinishTask?.Wait();

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
