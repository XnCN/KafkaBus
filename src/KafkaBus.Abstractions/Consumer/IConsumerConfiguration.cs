using Confluent.Kafka;

namespace KafkaBus.Abstractions.Consumer;

public interface IConsumerConfiguration<TKey, TMessage>
{
    int WorkerCount { get; }
    string Topic { get; }
    string GroupId { get; }
    Task<ConsumerConfig> ConfigureAsync();
    Task<ConsumerBuilder<TKey, TMessage>> GenerateBuilderAsync();
    IDeserializer<TKey>? KeySerializer => null;
    IDeserializer<TMessage>? ValueSerializer => null;
}