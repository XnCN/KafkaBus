using Confluent.Kafka;

namespace KafkaBus.Domain.Producer;

public delegate Task<DeliveryResult<TKey, TMessage>> ProduceDelegate<TKey, TMessage>(ProduceContext<TKey, TMessage> context, CancellationToken ct);