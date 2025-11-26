using System.Reflection.Metadata;

namespace Plugin.Logging
{
    public static class LogETLProcess
    {
        public static Serilog.Core.Logger Logger { get; set; }

        public static void Information(DateTime timestamp, string project, string message)
        {
            Logger
                .ForContext("Project", project)
                .Information(message); // log the plain message text
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
    }
}
