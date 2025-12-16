using System.Reflection.Metadata;

namespace Plugin.Logging
{
    public static class LogMigrationException
    {
        public static Serilog.Core.Logger Logger { get; set; }

        public static void Error(DateTime timestamp, string project, string message, Exception exception)
        {
            Logger
                .ForContext("Project", project)
                .Error(exception, message);
        }

        public static void Error(DateTime timestamp, string project, Exception exception)
        {
            Error(timestamp, project, exception.Message, exception);
        }
    }
}
