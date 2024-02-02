using Gauge.Messages;
using System.Text;

namespace ReportPortal.GaugePlugin.Extensions
{
    internal static class StepNameExtensions
    {
        public static string GetStepName(this ProtoStep step)
        {
            if (step.Fragments is not null)
            {
                var stepNameBuilder = new StringBuilder();

                foreach (var fragment in step.Fragments)
                {
                    if (fragment.FragmentType == Fragment.Types.FragmentType.Text)
                    {
                        stepNameBuilder.Append(fragment.Text);
                    }
                    else if (fragment.FragmentType == Fragment.Types.FragmentType.Parameter)
                    {
                        stepNameBuilder.AppendFormat("`{0}`", fragment.Parameter.Value);
                    }
                }

                return stepNameBuilder.ToString();
            }

            return step.ActualText;
        }
    }
}
