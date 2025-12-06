using System.Data;
using System.Text.Json;
using Dapper;
using Npgsql;
using PerformancePoc.Common;

namespace PerformancePoc.Db
{
    public class DbProducer : IProducer
    {
        private readonly string _connectionString;

        public DbProducer(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task PublishAsync(MessagePayload message)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var json = JsonSerializer.Serialize(message);
            
            await connection.ExecuteAsync(
                "INSERT INTO TmpMessages (Id, Payload, CreatedAt) VALUES (@Id, @Payload::jsonb, @CreatedAt)",
                new { message.Id, Payload = json, message.CreatedAt });
        }

        public async Task InitializeSchemaAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS TmpMessages (
                    Id UUID PRIMARY KEY,
                    Payload JSONB,
                    RetryCount INT DEFAULT 0,
                    CreatedAt TIMESTAMP DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS SupportCases (
                    Id UUID PRIMARY KEY,
                    Payload JSONB,
                    Reason TEXT,
                    CreatedAt TIMESTAMP DEFAULT NOW()
                );
            ");
        }
        
        public async Task CleanupAsync()
        {
             using var connection = new NpgsqlConnection(_connectionString);
             await connection.ExecuteAsync("TRUNCATE TABLE TmpMessages; TRUNCATE TABLE SupportCases;");
        }
    }
}
