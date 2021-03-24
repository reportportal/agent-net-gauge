using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReportPortal.GaugePlugin.Services;
using ReportPortal.Shared.Configuration;
using ReportPortal.Shared.Configuration.Providers;
using System;
using System.Linq;

namespace ReportPortal.GaugePlugin
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(options =>
            {
                options.MaxReceiveMessageSize = null;
            });

            services.AddSingleton(s =>
            {
                return new ConfigurationBuilder()
                  .Add(new EnvironmentVariablesConfigurationProvider("RP_", "_", EnvironmentVariableTarget.Process))
                  .Build();
            });

            services.AddSingleton<ReportPortalApiClientFactory>();

            services.AddSingleton(s =>
            {
                var factory = s.GetService<ReportPortalApiClientFactory>(); return factory.Create();
            });

            services.AddSingleton<Results.Sender>();
        }

        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime, IWebHostEnvironment env)
        {
            lifetime.ApplicationStarted.Register(() => BroadcastListeningAddressForGauge(app.ServerFeatures));

            app.UseRouting();

            var rpConfig = app.ApplicationServices.GetService<IConfiguration>();

            app.UseEndpoints(endpoints =>
            {
                if (rpConfig.GetValue("Enabled", true))
                {
                    endpoints.MapGrpcService<ReportMessagesHandler>();
                }
                else
                {
                    endpoints.MapGrpcService<EmptyMessagesHandler>();
                }
            });
        }

        public void BroadcastListeningAddressForGauge(IFeatureCollection features)
        {
            var addressFeature = features.Get<IServerAddressesFeature>();

            if (addressFeature != null)
            {
                foreach (var address in addressFeature.Addresses)
                {
                    var gaugePort = int.Parse(address.Split(":").Last());
                    Console.WriteLine($"Listening on port:{gaugePort}");
                }
            }
        }
    }
}
