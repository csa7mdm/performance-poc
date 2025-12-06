using System.Text;
using System.Text.Json;
using PerformancePoc.Common;
using RabbitMQ.Client;

namespace PerformancePoc.Mq
{
    public class MqProducer : IProducer, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private const string QueueName = "work_queue";
        private const string SupportQueueName = "support_queue";

        public MqProducer(string hostName)
        {
            var factory = new ConnectionFactory { HostName = hostName };
            _connection = factory.CreateConnectionAsync().Result;
            _channel = _connection.CreateChannelAsync().Result;
        }

        public async Task InitializeAsync()
        {
            await _channel.QueueDeclareAsync(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            await _channel.QueueDeclareAsync(queue: SupportQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        }
        
        public async Task PurgeAsync()
        {
            await _channel.QueuePurgeAsync(QueueName);
            await _channel.QueuePurgeAsync(SupportQueueName);
        }

        public async Task PublishAsync(MessagePayload message)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties();
            props.Persistent = true;

            await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: QueueName, mandatory: true, basicProperties: props, body: body);
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
