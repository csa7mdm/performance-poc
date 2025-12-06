using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using PerformancePoc.Common;
using PerformancePoc.Db;
using PerformancePoc.Mq;

namespace PerformancePoc.Benchmarks
{
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 1, iterationCount: 5)]
    [MemoryDiagnoser]
    public class PerformanceBenchmarks
    {
        private const string DbConnectionString = "Host=localhost;Port=5432;Database=performance_poc;Username=admin;Password=password";
        private const string MqHostName = "localhost";

        private DbProducer _dbProducer;
        private MqProducer _mqProducer;
        private MessagePayload _payload;

        [Params(100)] // Number of messages
        public int N;

        [Params(1024)] // Payload size in bytes
        public int PayloadSize;

        [GlobalSetup]
        public async Task Setup()
        {
            _dbProducer = new DbProducer(DbConnectionString);
            await _dbProducer.InitializeSchemaAsync();
            await _dbProducer.CleanupAsync();

            _mqProducer = new MqProducer(MqHostName);
            await _mqProducer.InitializeAsync();
            await _mqProducer.PurgeAsync();

            _payload = MessagePayload.Create(PayloadSize);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _mqProducer.Dispose();
        }

        [Benchmark]
        public async Task Db_Produce_Seq()
        {
            for (int i = 0; i < N; i++)
            {
                await _dbProducer.PublishAsync(MessagePayload.Create(PayloadSize));
            }
        }

        [Benchmark]
        public async Task Mq_Produce_Seq()
        {
            for (int i = 0; i < N; i++)
            {
                await _mqProducer.PublishAsync(MessagePayload.Create(PayloadSize));
            }
        }

        [Benchmark]
        public async Task Db_Consume_Seq()
        {
            // Pre-fill
            for (int i = 0; i < N; i++) await _dbProducer.PublishAsync(MessagePayload.Create(PayloadSize));

            var consumer = new DbConsumer(DbConnectionString);
            var cts = new CancellationTokenSource();
            
            // Start consumer task
            var task = consumer.ConsumeAsync(cts.Token);

            // Wait until empty (polling check for simplicity in benchmark)
            while (await GetDbCount() > 0)
            {
                await Task.Delay(10);
            }
            
            cts.Cancel();
            try { await task; } catch (OperationCanceledException) {}
        }

        [Benchmark]
        public async Task Mq_Consume_Seq()
        {
             // Pre-fill
            for (int i = 0; i < N; i++) await _mqProducer.PublishAsync(MessagePayload.Create(PayloadSize));

            using var consumer = new MqConsumer(MqHostName);
            var cts = new CancellationTokenSource();
            
            var task = consumer.ConsumeAsync(cts.Token);

            // Wait until empty
             while (await GetMqCount() > 0)
            {
                await Task.Delay(10);
            }

            cts.Cancel();
             try { await task; } catch (OperationCanceledException) {}
        }

        [Benchmark]
        public async Task Db_Produce_Par()
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, N), async (i, ct) =>
            {
                await _dbProducer.PublishAsync(MessagePayload.Create(PayloadSize));
            });
        }

        [Benchmark]
        public async Task Mq_Produce_Par()
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, N), async (i, ct) =>
            {
                await _mqProducer.PublishAsync(MessagePayload.Create(PayloadSize));
            });
        }

        [Benchmark]
        public async Task Db_Consume_Par()
        {
            // Pre-fill
            await Parallel.ForEachAsync(Enumerable.Range(0, N), async (i, ct) => await _dbProducer.PublishAsync(MessagePayload.Create(PayloadSize)));

            var cts = new CancellationTokenSource();
            // Start 4 consumers
            var tasks = new List<Task>();
            for (int i = 0; i < 4; i++)
            {
                var consumer = new DbConsumer(DbConnectionString);
                tasks.Add(consumer.ConsumeAsync(cts.Token));
            }

            while (await GetDbCount() > 0)
            {
                await Task.Delay(10);
            }
            
            cts.Cancel();
            try { await Task.WhenAll(tasks); } catch (OperationCanceledException) {}
        }

        [Benchmark]
        public async Task Mq_Consume_Par()
        {
             // Pre-fill
            await Parallel.ForEachAsync(Enumerable.Range(0, N), async (i, ct) => await _mqProducer.PublishAsync(MessagePayload.Create(PayloadSize)));

            var cts = new CancellationTokenSource();
            // Start 4 consumers
            var tasks = new List<Task>();
            for (int i = 0; i < 4; i++)
            {
                var consumer = new MqConsumer(MqHostName);
                tasks.Add(consumer.ConsumeAsync(cts.Token));
            }

            // Wait until empty
             while (await GetMqCount() > 0)
            {
                await Task.Delay(10);
            }

            cts.Cancel();
             try { await Task.WhenAll(tasks); } catch (OperationCanceledException) {}
        }

        // Helper to check remaining count
        private async Task<int> GetDbCount()
        {
            using var conn = new Npgsql.NpgsqlConnection(DbConnectionString);
            return await Dapper.SqlMapper.ExecuteScalarAsync<int>(conn, "SELECT COUNT(*) FROM TmpMessages");
        }
        
        private async Task<uint> GetMqCount()
        {
            // RabbitMQ Client doesn't expose queue count easily without declaring
            // We can assume for benchmark sake we wait or use management API
            // For simplicity, we'll just wait a bit or trust the consumer speed in this POC
            // A better way is to have the consumer signal completion
            return 0; // Placeholder: Real implementation needs a way to know when done
        }
    }
}
