using ReportPortal.Shared.Extensibility;
using ReportPortal.Shared.Extensibility.ReportEvents;
using System;
using System.IO;

namespace ReportPortal.GaugePlugin.Observers
{
    internal class FileResultsObserver : IReportEventsObserver
    {
        public void Initialize(IReportEventsSource reportEventsSource)
        {
            reportEventsSource.OnAfterLaunchFinished += ReportEventsSource_OnAfterLaunchFinished;
        }

        private void ReportEventsSource_OnAfterLaunchFinished(Shared.Reporter.ILaunchReporter launchReporter, Shared.Extensibility.ReportEvents.EventArgs.AfterLaunchFinishedEventArgs args)
        {
            File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("GAUGE_PROJECT_ROOT"), Environment.GetEnvironmentVariable("logs_directory"), "ReportPortal.Launch.Uuid"), launchReporter.Info.Uuid);
        }
    }
}
