namespace KafkaBus.Domain.Consumer;

public delegate Task ConsumeDelegate<TKey, TMessage>(ConsumeContext<TKey, TMessage> context, CancellationToken ct);
