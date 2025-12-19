using Confluent.Kafka;

namespace KafkaBus.Domain.Producer;

public interface IProducerConfiguration<TKey, TMessage>
{
    Task<ProducerConfig> ConfigureAsync();
    Task<ProducerBuilder<TKey, TMessage>> GenerateBuilderAsync();
    TKey GetKey(TMessage message);
    ISerializer<TKey>? KeySerializer => null;
    ISerializer<TMessage>? ValueSerializer => null;
}