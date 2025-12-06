using System.Threading.Tasks;

namespace PerformancePoc.Common
{
    public interface IProducer
    {
        Task PublishAsync(MessagePayload message);
    }

    public interface IConsumer
    {
        Task ConsumeAsync(CancellationToken cancellationToken);
    }
}
