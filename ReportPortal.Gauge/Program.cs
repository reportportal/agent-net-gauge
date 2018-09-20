using Gauge.CSharp.Core;
using Gauge.Messages;
using System;
using System.Diagnostics;
using System.Linq;

namespace ReportPortal.Gauge
{
    class Program
    {
        static void Main(string[] args)
        {
            var port = Convert.ToInt32(Environment.GetEnvironmentVariable("plugin_connection_port"));

            using (var gaugeConnection = new GaugeConnection(new TcpClientWrapper(port)))
            {
                while (gaugeConnection.Connected)
                {
                    var message = Message.Parser.ParseFrom(gaugeConnection.ReadBytes().ToArray());

                    if (message.MessageType == Message.Types.MessageType.SuiteExecutionResult)
                    {
                        Console.Write("Finishing to send results to Report Portal... ");
                        Console.WriteLine("Done.");

                        var suiteResult = message.SuiteExecutionResult.SuiteResult;
                    }

                    if (message.MessageType == Message.Types.MessageType.KillProcessRequest)
                        return;
                }
            }
        }
    }
}
