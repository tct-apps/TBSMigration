using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PushTripTOS.Model;
using Serilog;
using Serilog.Settings.Configuration;
using Serilog.Sinks.MSSqlServer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            #region Load configuration
            // Load configuration from appsettings
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Initialize Serilog logger(s) from configuration
            //LogETLProcess.Logger = new LoggerConfiguration()
            //    .ReadFrom.Configuration(config, sectionName: "Serilog_ETLProcess")
            //    .CreateLogger();
            //LogETLException.Logger = new LoggerConfiguration()
            //    .ReadFrom.Configuration(config, sectionName: "Serilog_ETLException")
            //    .CreateLogger();

            string sourceConn = config.GetConnectionString("Source");
            #endregion

            State(sourceConn);
            City(sourceConn);
            Vehicle(sourceConn);
        }
        catch (Exception ex)
        {
            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            //LogETLException.Error(ts, "Main", "Unhandled exception in Main()", ex);
        }
        finally
        {
            // Ensure logs are flushed before exit
            //(LogETLProcess.Logger as IDisposable)?.Dispose();
            //(LogETLException.Logger as IDisposable)?.Dispose();
        }
    }

    static void State(string sourceConn)
    {
        var logs = new List<(DateTime TimeStamp, string Project, string Message)>();
        var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

        // Log process start
        logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"StateStart", $"State process started"));

        try
        {
            string sqlPath = Path.Combine(
                             Directory.GetCurrentDirectory(),
                             "Model", "SQL", "State.sql");

            string sql = File.ReadAllText(sqlPath);

            using var source = new SqlConnection(sourceConn);
            source.Open();

            using var multi = source.QueryMultiple(sql);

            List<StateModel> stateList;

            // Read
            try
            {
                stateList = multi.Read<StateModel>().ToList();

                // logging process read
                logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"StateRead", $"State Read process started"));
            }
            catch (Exception ex)
            {
                var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
                //LogETLException.Error(ts, $"{cts}FactTicketRead", "Exception during Read phase", ex);
                throw;
            }

            // Write process logs
            //LogETLProcess.WriteAll(logs);
        }
        catch (Exception ex)
        {
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            //LogETLException.Error(ts, $"{cts}FactTicketOverall", "Unhandled exception in FactTicket() overall", ex);
            throw;
        }

        //New table last jalan bila dan masa jalan tu ada error tak
        //Obj console app proses n see if success or has any error
    }

    static void City(string sourceConn)
    {
        var logs = new List<(DateTime TimeStamp, string Project, string Message)>();
        var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

        // Log process start
        logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"CityStart", $"City process started"));

        try
        {
            string sqlPath = Path.Combine(
                             Directory.GetCurrentDirectory(),
                             "Model", "SQL", "City.sql");

            string sql = File.ReadAllText(sqlPath);

            using var source = new SqlConnection(sourceConn);
            source.Open();

            using var multi = source.QueryMultiple(sql);

            List<CityModel> cityList;

            // Read
            try
            {
                cityList = multi.Read<CityModel>().ToList();

                // logging process read
                logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"CityRead", $"City Read process started"));
            }
            catch (Exception ex)
            {
                var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
                //LogETLException.Error(ts, $"{cts}FactTicketRead", "Exception during Read phase", ex);
                throw;
            }

            // Write process logs
            //LogETLProcess.WriteAll(logs);
        }
        catch (Exception ex)
        {
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            //LogETLException.Error(ts, $"{cts}FactTicketOverall", "Unhandled exception in FactTicket() overall", ex);
            throw;
        }

        //New table last jalan bila dan masa jalan tu ada error tak
        //Obj console app proses n see if success or has any error
    }

    static void Vehicle(string sourceConn)
    {
        var logs = new List<(DateTime TimeStamp, string Project, string Message)>();
        var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");

        // Log process start
        logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"VehicleStart", $"Vehicle process started"));

        try
        {
            string sqlPath = Path.Combine(
                             Directory.GetCurrentDirectory(),
                             "Model", "SQL", "Vehicle.sql");

            string sql = File.ReadAllText(sqlPath);

            using var source = new SqlConnection(sourceConn);
            source.Open();

            using var multi = source.QueryMultiple(sql);

            List<VehicleModel> vehicleList;

            // Read
            try
            {
                vehicleList = multi.Read<VehicleModel>().ToList();

                // logging process read
                logs.Add((TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone), $"VehicleRead", $"Vehicle Read process started"));
            }
            catch (Exception ex)
            {
                var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
                //LogETLException.Error(ts, $"{cts}FactTicketRead", "Exception during Read phase", ex);
                throw;
            }

            // Write process logs
            //LogETLProcess.WriteAll(logs);
        }
        catch (Exception ex)
        {
            var ts = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            //LogETLException.Error(ts, $"{cts}FactTicketOverall", "Unhandled exception in FactTicket() overall", ex);
            throw;
        }

        //New table last jalan bila dan masa jalan tu ada error tak
        //Obj console app proses n see if success or has any error
    }
}
