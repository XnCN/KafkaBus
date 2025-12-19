using Confluent.Kafka;

namespace KafkaBus.Domain.Producer;

public sealed record ProduceContext<TKey, TMessage>(TKey Key, TMessage Message, string Topic, int? Partition, Headers? Headers);
