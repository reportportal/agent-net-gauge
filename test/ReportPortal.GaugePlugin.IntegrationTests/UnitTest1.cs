using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using Xunit;

namespace ReportPortal.GaugePlugin.IntegrationTests
{
    public class UnitTest1 : IClassFixture<WebApplicationFactory<Startup>>, IDisposable
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public UnitTest1(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        public void Dispose()
        {
            _factory.Dispose();
        }

        [Fact]
        public void Test1()
        {
            Environment.SetEnvironmentVariable("rp_enabled", "false");

            var client = _factory.CreateDefaultClient();
            client.MaxResponseContentBufferSize = int.MaxValue;

            GrpcChannel ch = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
            {
                HttpClient = client,
                MaxReceiveMessageSize = null
            });

            var service = new Gauge.Messages.Reporter.ReporterClient(ch);

            service.NotifyExecutionStarting(
                new Gauge.Messages.ExecutionStartingRequest()
                {
                    CurrentExecutionInfo = new Gauge.Messages.ExecutionInfo
                    {
                        ProjectName = new string('A', 150 * 1024 * 1024)
                    }
                });
        }
    }
}
