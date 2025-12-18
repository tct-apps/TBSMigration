using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

class Program
{
    static async Task Main()
    {
        // Load configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string sourceConn = config.GetConnectionString("Source");

        bool isConnected = await TestConnectionAsync(sourceConn);

        if (!isConnected)
        {
            Console.WriteLine("Connection FAILED");
            return;
        }

        Console.WriteLine("Connection SUCCESS");

        // continue your process here
    }

    static async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            Console.WriteLine("Connected to: " + conn.DataSource);
            Console.WriteLine("Database: " + conn.Database);
            return conn.State == ConnectionState.Open;
        }
        catch (SqlException ex)
        {
            Console.WriteLine("SQL Exception: " + ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine("General Exception: " + ex.Message);
            return false;
        }
    }
}
