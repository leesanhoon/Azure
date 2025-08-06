using Microsoft.Data.SqlClient;
using System;

class Program
{
    static async Task Main(string[] args)
    {
        var connectionString = "Server=tcp:netcoreazureninh.database.windows.net,1433;Initial Catalog=netcoreazure;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;";

        Console.WriteLine("Testing Azure SQL Database connection...");
        Console.WriteLine($"Connection string: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            Console.WriteLine("✅ Connection successful!");

            using var command = new SqlCommand("SELECT @@VERSION", connection);
            var version = await command.ExecuteScalarAsync();
            Console.WriteLine($"SQL Server version: {version}");

            // Test if database exists and has tables
            using var tableCommand = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES", connection);
            var tableCount = await tableCommand.ExecuteScalarAsync();
            Console.WriteLine($"Number of tables in database: {tableCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Connection failed: {ex.Message}");
            Console.WriteLine($"Error type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }
}