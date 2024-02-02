using Gauge.Messages;
using System;
using System.Linq;

namespace ReportPortal.GaugePlugin.Extensions
{
    internal static class ProtoExtensions
    {
        public static string GetStepName(this ExecuteStepRequest step)
        {
            if (step.Parameters is not null)
            {
                var stepName = step.ParsedStepText;

                foreach (var parameter in step.Parameters)
                {
                    var startIndex = stepName.IndexOf("{}");

                    var parameterValue = parameter.ParameterType switch
                    {
                        Parameter.Types.ParameterType.Table => parameter.Name,
                        Parameter.Types.ParameterType.SpecialTable => parameter.Name,
                        _ => parameter.Value,
                    };

                    stepName = stepName.Remove(startIndex, 2);

                    if (!string.IsNullOrEmpty(parameterValue))
                    {
                        stepName = stepName.Insert(startIndex, $"`{parameterValue}`");
                    }
                }

                return stepName;
            }
            else
            {
                return step.ActualStepText;
            }
        }

        public static string AsMarkdown(this ProtoTable table)
        {
            var text = "| **" + string.Join("** | **", table.Headers.Cells.ToArray()) + "** |";
            text += Environment.NewLine + "| " + string.Join(" | ", table.Headers.Cells.Select(c => "---")) + " |";

            foreach (var tableRow in table.Rows)
            {
                text += Environment.NewLine + "| " + string.Join(" | ", tableRow.Cells.ToArray()) + " |";
            }

            return text;
        }
    }
}
