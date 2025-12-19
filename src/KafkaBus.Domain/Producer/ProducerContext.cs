using Confluent.Kafka;

namespace KafkaBus.Domain.Producer;

public sealed record ProducerContext<TKey, TMessage>(IProducer<TKey, TMessage> Producer, IProducerConfiguration<TKey, TMessage> Configuration);