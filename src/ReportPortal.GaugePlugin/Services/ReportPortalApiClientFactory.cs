using ReportPortal.Client;
using ReportPortal.Client.Abstractions;
using ReportPortal.Shared.Configuration;
using System;

namespace ReportPortal.GaugePlugin.Services
{
    public class ReportPortalApiClientFactory
    {
        private IConfiguration _configuration;

        public ReportPortalApiClientFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IClientService Create()
        {
            var rpUri = _configuration.GetValue<string>("Uri", null) ?? _configuration.GetValue<string>("Url");
            var rpProject = _configuration.GetValue<string>("Project");
            var rpApiToken = _configuration.GetValue<string>("Uuid");
            return new Service(new Uri(rpUri), rpProject, rpApiToken);
        }
    }
}
