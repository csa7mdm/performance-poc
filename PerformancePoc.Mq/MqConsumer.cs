using System.Text;
using System.Text.Json;
using PerformancePoc.Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PerformancePoc.Mq
{
    public class MqConsumer : IConsumer, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly bool _simulateFailure;
        private const string QueueName = "work_queue";
        private const string SupportQueueName = "support_queue";

        public MqConsumer(string hostName, bool simulateFailure = false)
        {
            _simulateFailure = simulateFailure;
            var factory = new ConnectionFactory { HostName = hostName };
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
        }

        public async Task ConsumeAsync(CancellationToken cancellationToken)
        {
            await _channel.QueueDeclareAsync(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                // Get Retry Count from Headers
                int retryCount = 0;
                if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("x-retry-count"))
                {
                    if (ea.BasicProperties.Headers["x-retry-count"] is int count)
                    {
                        retryCount = count;
                    }
                }

                try
                {
                    await ProcessMessageAsync(message, _simulateFailure);
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception)
                {
                    if (retryCount >= 3)
                    {
                        // Move to Support Queue
                        var props = new BasicProperties { Persistent = true };
                        await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: SupportQueueName, mandatory: true, basicProperties: props, body: body);
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        // Retry (Re-publish with incremented header)
                        var props = new BasicProperties { Persistent = true };
                        props.Headers = new Dictionary<string, object?>
                        {
                            { "x-retry-count", retryCount + 1 }
                        };
                        
                        // Republish to tail of queue (simple retry)
                        await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: QueueName, mandatory: true, basicProperties: props, body: body);
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                }
            };

            await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer);

            // Keep alive until cancelled
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Expected
            }
        }

        private Task ProcessMessageAsync(string payloadJson, bool fail)
        {
            if (fail)
            {
                throw new Exception("Simulated Processing Failure");
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.CloseAsync().Wait();
            _connection?.CloseAsync().Wait();
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
