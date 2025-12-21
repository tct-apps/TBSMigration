using Serilog.Events;
using Serilog.Parsing;
using System.Reflection.Metadata;

namespace Plugin.Logging
{
    public static class LogMigrationProcess
    {
        public static Serilog.Core.Logger Logger { get; set; }

        public static void Information(string type, string process, string message, string requestXml, string responseXml, string customData, bool? isSuccess)
        {
            Logger
                .ForContext("Type", type)
                .ForContext("Process", process)
                .ForContext("IsSuccess", isSuccess)
                .ForContext("RequestXml", requestXml)
                .ForContext("ResponseXml", responseXml)
                .ForContext("CustomData", customData)
                .Information(message);
        }

        public static void WriteAll(IEnumerable<(DateTime TimeStamp, string Type, string Process, string Message,
                 string RequestXml, string ResponseXml, string CustomData, bool? IsSuccess)> logs)
        {
            var parser = new MessageTemplateParser();

            foreach (var log in logs)
            {
                 var props = new List<LogEventProperty>
                {
                    new("Type", new ScalarValue(log.Type)),
                    new("Process", new ScalarValue(log.Process)),
                    new("RequestXml", new ScalarValue(log.RequestXml)),
                    new("ResponseXml", new ScalarValue(log.ResponseXml)),
                    new("CustomData", new ScalarValue(log.CustomData))
                };

                if (log.IsSuccess.HasValue)
                    props.Add(new LogEventProperty("IsSuccess", new ScalarValue(log.IsSuccess)));

                var logEvent = new LogEvent(
                    timestamp: new DateTimeOffset(log.TimeStamp), // 🔒 YOUR TIME, NOT REPLACED
                    level: LogEventLevel.Information,
                    exception: null,
                    messageTemplate: parser.Parse(log.Message),
                    properties: props
                );

                Logger.Write(logEvent);
            }
        }

    }
}
