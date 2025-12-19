using Confluent.Kafka;

namespace KafkaBus.Shared.Consumer;

public sealed record ConsumeContext<TKey, TMessage>(TKey Key, TMessage Message, string Topic, int Partition, long Offset, Headers? Headers, DateTime Timestamp, TopicPartitionOffset TopicPartitionOffset);