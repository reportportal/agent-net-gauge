using Gauge.Messages;

namespace ReportPortal.GaugePlugin.Extensions
{
    internal static class StepNameExtensions
    {
        public static string GetStepName(this ExecuteStepRequest step)
        {
            if (step.Parameters is not null)
            {
                var stepName = step.ParsedStepText;

                foreach (var parameter in step.Parameters)
                {
                    var startIndex = stepName.IndexOf("{}");

                    stepName = stepName.Remove(startIndex, 2).Insert(startIndex, $"`{parameter.Value}`");
                }

                return stepName;
            }
            else
            {
                return step.ActualStepText;
            }
        }
    }
}
