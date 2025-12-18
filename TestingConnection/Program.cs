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
            return conn.State == ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }
}
