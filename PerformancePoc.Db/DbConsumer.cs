using System.Data;
using System.Text.Json;
using Dapper;
using Npgsql;
using PerformancePoc.Common;

namespace PerformancePoc.Db
{
    public class DbConsumer : IConsumer
    {
        private readonly string _connectionString;
        private readonly bool _simulateFailure;

        public DbConsumer(string connectionString, bool simulateFailure = false)
        {
            _connectionString = connectionString;
            _simulateFailure = simulateFailure;
        }

        private class MessageRow
        {
            public Guid Id { get; set; }
            public string Payload { get; set; }
            public int RetryCount { get; set; }
        }

        public async Task ConsumeAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var transaction = await connection.BeginTransactionAsync(cancellationToken);

                // Fetch one message (simulating queue behavior with SKIP LOCKED)
                var message = await connection.QueryFirstOrDefaultAsync<MessageRow>(
                    @"SELECT Id, Payload, RetryCount FROM TmpMessages 
                      ORDER BY CreatedAt 
                      FOR UPDATE SKIP LOCKED 
                      LIMIT 1", transaction: transaction);

                if (message == null)
                {
                    await Task.Delay(10, cancellationToken); // Backoff if empty
                    continue;
                }

                try
                {
                    await ProcessMessageAsync(message.Payload, _simulateFailure);
                    
                    // Success: Delete
                    await connection.ExecuteAsync(
                        "DELETE FROM TmpMessages WHERE Id = @Id", 
                        new { message.Id }, transaction: transaction);
                }
                catch (Exception)
                {
                    if (message.RetryCount >= 3)
                    {
                        // Move to Support
                        await connection.ExecuteAsync(
                            "INSERT INTO SupportCases (Id, Payload, Reason) VALUES (@Id, @Payload::jsonb, 'Max Retries Exceeded')",
                            new { message.Id, message.Payload }, transaction: transaction);
                        
                        await connection.ExecuteAsync(
                            "DELETE FROM TmpMessages WHERE Id = @Id", 
                            new { message.Id }, transaction: transaction);
                    }
                    else
                    {
                        // Increment Retry
                        await connection.ExecuteAsync(
                            "UPDATE TmpMessages SET RetryCount = RetryCount + 1 WHERE Id = @Id",
                            new { message.Id }, transaction: transaction);
                    }
                }

                await transaction.CommitAsync(cancellationToken);
            }
        }

        private Task ProcessMessageAsync(string payloadJson, bool fail)
        {
            // Placeholder processing
            if (fail)
            {
                throw new Exception("Simulated Processing Failure");
            }
            return Task.CompletedTask;
        }
    }
}
