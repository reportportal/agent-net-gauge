using Gauge.Messages;
using ReportPortal.Client.Abstractions.Models;
using ReportPortal.Client.Abstractions.Requests;
using ReportPortal.Shared.Reporter;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace ReportPortal.GaugePlugin.Results
{
    partial class Sender
    {
        private readonly object _lockObj = new();

        private int _launchesCount;

        private ILaunchReporter _launch;

        private StartLaunchRequest _startLaunchRequest;

        public void StartLaunch(ExecutionStartingRequest request)
        {
            lock (_lockObj)
            {
                if (_launch == null)
                {
                    var suiteExecutionResult = request.SuiteResult;
                    var isDebug = _configuration.GetValue("Launch:DebugMode", false);

                    // deffer starting of launch
                    _startLaunchRequest = new StartLaunchRequest
                    {
                        Name = _configuration.GetValue("Launch:Name", suiteExecutionResult.ProjectName),
                        Description = _configuration.GetValue("Launch:Description", string.Empty),
                        Attributes = _configuration.GetKeyValues("Launch:Attributes", new List<KeyValuePair<string, string>>()).Select(a => new Client.Abstractions.Models.ItemAttribute { Key = a.Key, Value = a.Value }).ToList(),
                        Mode = isDebug ? LaunchMode.Debug : LaunchMode.Default,
                        StartTime = DateTime.UtcNow                        
                    };
                }

                _launchesCount++;
            }
        }

        public void FinishLaunch(ExecutionEndingRequest request)
        {
            lock (_lockObj)
            {
                _launchesCount--;

                if (_launchesCount == 0)
                {
                    var launchLogs = Environment.GetEnvironmentVariable("rp_launch_logs") ?? "";
                    AttachFiles(launchLogs, LogLevel.Error, LaunchReporter);

                    _launch.Finish(new FinishLaunchRequest
                    {
                        EndTime = DateTime.UtcNow
                    });
                }
            }
        }

        public void Sync()
        {
            lock (_lockObj)
            {
                if (_launchesCount == 0)
                {
                    if (_launch is null)
                    {
                        throw new NullReferenceException("Launch cannot be null. Check logs folder to find potential problems.");
                    }
                    else
                    {
                        _launch.Sync();
                    }
                }
            }
        }

        public ILaunchReporter LaunchReporter => _launch;

        private void AttachFiles(String attachPattern, LogLevel logLevel, ILaunchReporter testReporter)
        {
            if (string.IsNullOrEmpty(attachPattern)) return;

            var gaugeProjectRoot = Environment.GetEnvironmentVariable("GAUGE_PROJECT_ROOT") ?? Environment.CurrentDirectory;
            List<string> patterns = attachPattern.Split(',').Select(word => word.Trim()).Where(word => !string.IsNullOrEmpty(word)).ToList();

            foreach (var pattern in patterns) {
                string[] attachFiles = null;
                try {
                    attachFiles = Directory.GetFiles(gaugeProjectRoot, pattern);
                }
                catch (Exception exp){
                    TraceLogger.Error(@$"Pattern '{pattern}' error: {exp}");
                }
                foreach (var attachFile in attachFiles) {
                    try
                    {
                        if (!string.IsNullOrEmpty(attachFile))
                        {
                            var mimeType = Shared.MimeTypes.MimeTypeMap.GetMimeType(Path.GetExtension(attachFile));
                            var name = Path.GetFileName(attachFile);

                            testReporter.Log(new CreateLogItemRequest
                            {
                                Time = DateTime.UtcNow,
                                Level = logLevel,
                                Text = name,
                                Attach = new LogItemAttach
                                {
                                    Name = name,
                                    MimeType = mimeType,
                                    Data = LoadFile(attachFile)
                                }
                            });
                        }
                    }
                    catch (Exception exp)
                    {
                        TraceLogger.Error(@$"Couldn't attach file: '{attachFile}'. Error: {exp}");
                    }
                }
            }
        }

        private byte[] LoadFile(String loadFile)
        {
            /// Load the file data in a FileStream as FileShare.ReadWrite
            /// in case the file is still being updated by another process.
            /// The file must not be locked.

            byte[] data=null;
            try
            {
                using(FileStream fileStream = new FileStream(loadFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using(StreamReader streamReader = new StreamReader(fileStream))
                    {
                        var lString = streamReader.ReadToEnd();
                        data = Encoding.UTF8.GetBytes(lString);
                    }
                }
            }
            catch (Exception exp)
            {
                TraceLogger.Error(@$"Couldn't open and read file: '{loadFile}'. Error: {exp}");
            }
            return data;
        } 

    }
}
