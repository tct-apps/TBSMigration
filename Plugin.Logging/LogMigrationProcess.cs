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

        public static void WriteAll(IEnumerable<(DateTime TimeStamp, string Type, string Process, string Message, string RequestXml, string ResponseXml, string CustomData, bool? IsSuccess)> logs)
        {
            var logEvents = logs.Select(log => new
            {
                log.TimeStamp,
                log.Type,
                log.Process,
                log.IsSuccess,
                log.RequestXml,
                log.ResponseXml,
                log.CustomData,
                log.Message
            }).ToList();

            foreach (var log in logEvents)
            {
                Logger
                    .ForContext("Type", log.Type)
                    .ForContext("Process", log.Process)
                    .ForContext("IsSuccess", log.IsSuccess)
                    .ForContext("RequestXml", log.RequestXml)
                    .ForContext("ResponseXml", log.ResponseXml)
                    .ForContext("CustomData", log.CustomData)
                    .Information(log.Message);
            }
        }

    }
}
