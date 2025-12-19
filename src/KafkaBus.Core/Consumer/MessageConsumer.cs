using KafkaBus.Abstractions.Consumer;
using KafkaBus.Domain.Consumer;

namespace KafkaBus.Core.Consumer;

public abstract class MessageConsumer<TKey, TMessage> : IMessageConsumer<TKey, TMessage>
{
    public abstract Task HandleAsync(ConsumeContext<TKey, TMessage> context, CancellationToken ct = default);
}

public abstract class MessageConsumer<TMessage> : IMessageConsumer<string, TMessage>
{
    public abstract Task HandleAsync(ConsumeContext<string, TMessage> context, CancellationToken ct = default);
}