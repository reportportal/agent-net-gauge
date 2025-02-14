using ReportPortal.Client;
using ReportPortal.Client.Abstractions;
using ReportPortal.Shared.Configuration;
using System;

namespace ReportPortal.GaugePlugin.Services
{
    public class ReportPortalApiClientFactory(IConfiguration configuration)
    {
        public IClientService Create()
        {
            var rpUri = configuration.GetValue<string>("Uri", null!) ?? configuration.GetValue<string>("Url");
            var rpProject = configuration.GetValue<string>("Project");
            var rpApiToken = configuration.GetValue<string>("Uuid");
            return new Service(new Uri(rpUri), rpProject, rpApiToken);
        }
    }
}
