using KafkaBus.Domain.Consumer;

namespace KafkaBus.Abstractions.Consumer;

public interface IMessageConsumer<TKey, TMessage>
{
    Task HandleAsync(ConsumeContext<TKey, TMessage> context, CancellationToken ct = default);
}

public interface IMessageConsumer<TMessage> : IMessageConsumer<string, TMessage> { }