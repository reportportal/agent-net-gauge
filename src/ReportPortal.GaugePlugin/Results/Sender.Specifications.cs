using Gauge.Messages;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Client.Abstractions.Responses;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReportPortal.GaugePlugin.Results
{
    partial class Sender
    {
        private Dictionary<string, ITestReporter> _specs = new Dictionary<string, ITestReporter>();

        public void StartSpec(SpecExecutionStartingRequest request)
        {
            var specResult = request.SpecResult;

            var specReporter = _launchReporter.StartChildTestReporter(new StartTestItemRequest
            {
                Type = TestItemType.Suite,
                Name = specResult.ProtoSpec.SpecHeading,
                Description = string.Join("", specResult.ProtoSpec.Items.Where(i => i.ItemType == ProtoItem.Types.ItemType.Comment).Select(c => c.Comment.Text)),
                StartTime = DateTime.UtcNow,
                Tags = specResult.ProtoSpec.Tags.Select(t => t.ToString()).ToList()
            });

            var key = Newtonsoft.Json.JsonConvert.SerializeObject(request.CurrentExecutionInfo.CurrentSpec);
            _specs[key] = specReporter;
        }

        public void FinishSpec(SpecExecutionEndingRequest request)
        {
            var key = Newtonsoft.Json.JsonConvert.SerializeObject(request.CurrentExecutionInfo.CurrentSpec);

            _specs[key].Finish(new FinishTestItemRequest
            {
                EndTime = DateTime.UtcNow
            });

            _specs.Remove(key);
        }
    }
}
