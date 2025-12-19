using Confluent.Kafka;
using KafkaBus.Domain.Producer;

namespace KafkaBus.Abstractions.Producer;

public interface IProduceMiddleware<TKey, TMessage>
{
    Task<DeliveryResult<TKey, TMessage>> InvokeAsync(ProduceContext<TKey, TMessage> context, ProduceDelegate<TKey, TMessage> next, CancellationToken ct);
}