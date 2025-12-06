using PerformancePoc.Common;
using PerformancePoc.Db;
using PerformancePoc.Mq;
using RabbitMQ.Client;
using Dapper;
using Npgsql;

namespace PerformancePoc.Benchmarks
{
    public class FunctionalTest
    {
        private const string DbConnectionString = "Host=localhost;Port=5432;Database=performance_poc;Username=admin;Password=password";
        private const string MqHostName = "localhost";

        public static async Task RunAsync()
        {
            Console.WriteLine("Starting Functional Verification...");

            // 1. Setup
            var dbProducer = new DbProducer(DbConnectionString);
            await dbProducer.InitializeSchemaAsync();
            await dbProducer.CleanupAsync();

            var mqProducer = new MqProducer(MqHostName);
            await mqProducer.InitializeAsync();
            await mqProducer.PurgeAsync();

            // 2. Test DB Retry & Support Move
            Console.WriteLine("Testing DB Retry Logic...");
            var payload = MessagePayload.Create(100);
            await dbProducer.PublishAsync(payload);

            var dbConsumer = new DbConsumer(DbConnectionString, simulateFailure: true);
            var cts = new CancellationTokenSource();
            
            // Run consumer for enough time to fail 3 times and move
            var dbTask = dbConsumer.ConsumeAsync(cts.Token);
            
            // Wait for retries (backoff is small in code, but let's give it 2 seconds)
            await Task.Delay(2000); 
            cts.Cancel();
            try { await dbTask; } catch (OperationCanceledException) {}

            // Verify
            using var conn = new NpgsqlConnection(DbConnectionString);
            var supportCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SupportCases");
            var tmpCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM TmpMessages");

            if (supportCount == 1 && tmpCount == 0)
            {
                Console.WriteLine("SUCCESS: DB Message moved to SupportCases after failures.");
            }
            else
            {
                Console.WriteLine($"FAILURE: DB SupportCount={supportCount}, TmpCount={tmpCount}");
            }

            // 3. Test MQ Retry & Support Move
            Console.WriteLine("Testing MQ Retry Logic...");
            await mqProducer.PublishAsync(payload);

            using var mqConsumer = new MqConsumer(MqHostName, simulateFailure: true);
            var mqCts = new CancellationTokenSource();
            
            var mqTask = mqConsumer.ConsumeAsync(mqCts.Token);
            
            // Wait for retries
            await Task.Delay(2000);
            mqCts.Cancel();
            try { await mqTask; } catch (OperationCanceledException) {}

            // Verify MQ (Check Support Queue Count)
            // We need a temporary channel to check count
            var factory = new ConnectionFactory { HostName = MqHostName };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();
            
            var supportQueue = await channel.QueueDeclareAsync("support_queue", durable: true, exclusive: false, autoDelete: false);
            var workQueue = await channel.QueueDeclareAsync("work_queue", durable: true, exclusive: false, autoDelete: false);

            if (supportQueue.MessageCount == 1 && workQueue.MessageCount == 0)
            {
                Console.WriteLine("SUCCESS: MQ Message moved to support_queue after failures.");
            }
            else
            {
                Console.WriteLine($"FAILURE: MQ SupportQueueCount={supportQueue.MessageCount}, WorkQueueCount={workQueue.MessageCount}");
            }
            
            Console.WriteLine("Functional Verification Complete.");
        }
    }
}
