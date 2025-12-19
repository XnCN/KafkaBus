using KafkaBus.Domain.Consumer;
using KafkaBus.Shared.Consumer;

namespace KafkaBus.Abstractions.Consumer;

public interface IConsumeMiddleware<TKey, TMessage>
{
    Task InvokeAsync(ConsumeContext<TKey, TMessage> context, ConsumeDelegate<TKey, TMessage> next, CancellationToken ct);
}