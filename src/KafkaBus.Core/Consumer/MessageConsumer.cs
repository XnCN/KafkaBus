using Confluent.Kafka;
using KafkaBus.Abstractions.Consumer;
using KafkaBus.Shared.Consumer;

namespace KafkaBus.Core.Consumer;

public abstract class MessageConsumer<TKey, TMessage> : IMessageConsumer<TKey, TMessage>
{
    private IConsumer<TKey, TMessage>? _consumer;
    internal void SetConsumer(IConsumer<TKey, TMessage> consumer) => _consumer = consumer;
    protected void Ack(ConsumeContext<TKey, TMessage> context)
    {
        if (_consumer is null)
            throw new InvalidOperationException("Consumer not initialized");

        _consumer.Commit([context.TopicPartitionOffset]);
    }

    protected void Ack(IEnumerable<ConsumeContext<TKey, TMessage>> contexts)
    {
        if (_consumer is null)
            throw new InvalidOperationException("Consumer not initialized");

        var offsets = contexts.Select(c => c.TopicPartitionOffset).ToList();
        if (offsets.Count > 0)
            _consumer.Commit(offsets);
    }

    public abstract Task HandleAsync(ConsumeContext<TKey, TMessage> context, CancellationToken ct = default);
}

public abstract class MessageConsumer<TMessage> : MessageConsumer<string, TMessage>
{
}