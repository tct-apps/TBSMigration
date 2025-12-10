using System.Reflection.Metadata;

namespace Plugin.Logging
{
    public static class LogETLProcess
    {
        public static Serilog.Core.Logger Logger { get; set; }

        public static void Information(string type, string process, string message, string requestXml, string responseXml, bool? isSuccess)
        {
            Logger
                .ForContext("Type", type)
                .ForContext("Process", process)
                .ForContext("IsSuccess", isSuccess)
                .ForContext("RequestXml", requestXml)
                .ForContext("ResponseXml", responseXml)
                .Information(message);
        }

        public static void WriteAll(IEnumerable<(DateTime TimeStamp, string Project, string Message)> logs)
        {
            // Create a list of log events and push to Serilog in one call
            var logEvents = logs.Select(log => new
            {
                log.TimeStamp,
                log.Project,
                log.Message
            }).ToList();

            foreach (var log in logEvents)
            {
                Logger
                    .ForContext("Project", log.Project)
                    .Information(log.Message);
            }
        }

        public static void WriteAllTOS(IEnumerable<(DateTime TimeStamp, string Type, string Process, string Message, string RequestXml, string ResponseXml, bool? IsSuccess)> logs)
        {
            var logEvents = logs.Select(log => new
            {
                log.TimeStamp,
                log.Type,
                log.Process,
                log.IsSuccess,
                log.RequestXml,
                log.ResponseXml,
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
                    .Information(log.Message);
            }
        }

    }
}
