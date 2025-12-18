
namespace Plugin.Logging
{
    public static class LogMigrationException
    {
        public static Serilog.Core.Logger Logger { get; set; }

        public static void Error(DateTime timestamp,string type, string process,string requestXml, string responseXml,string customData,string message,Exception exception)
        {
            if (Logger == null)
            {
                Console.WriteLine("LOGGER NOT INITIALIZED");
                Console.WriteLine(message);
                Console.WriteLine(exception);
                return;
            }

            Logger
                .ForContext("Type", type)
                .ForContext("Process", process)
                .ForContext("RequestXml", requestXml)
                .ForContext("ResponseXml", responseXml)
                .ForContext("CustomData", customData)
                .Error(exception, message);
        }


        public static void Error(DateTime timestamp, string type, string process, string requestXml, string responseXml, string customData, Exception exception)
        {
            Error(timestamp, type, process, requestXml, responseXml, customData, exception.Message, exception);
        }
    }
}
