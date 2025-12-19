---
sidebar_position: 5
---

# Consumer Middleware

Middleware allows you to intercept messages before they reach your handler.

## Creating Middleware

Implement `IConsumeMiddleware<TKey, TMessage>`:

```csharp
[MiddlewareOrder(1)]
public class LoggingMiddleware<TKey, TMessage>(
    ILogger<LoggingMiddleware<TKey, TMessage>> logger)
    : IConsumeMiddleware<TKey, TMessage>
{
    public async Task InvokeAsync(
        ConsumeContext<TKey, TMessage> context,
        ConsumeDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        logger.LogInformation(
            "Consuming from {Topic} P:{Partition} O:{Offset}",
            context.Topic,
            context.Partition,
            context.Offset
        );

        await next(context, ct);

        logger.LogInformation("Consumed in {Elapsed}ms", sw.ElapsedMilliseconds);
    }
}
```

## ConsumeContext

Access message details in middleware:

```csharp
public async Task InvokeAsync(
    ConsumeContext<TKey, TMessage> context,
    ConsumeDelegate<TKey, TMessage> next,
    CancellationToken ct)
{
    var topic = context.Topic;
    var partition = context.Partition;
    var offset = context.Offset;
    var key = context.Key;
    var message = context.Message;
    var headers = context.Headers;
    var timestamp = context.Timestamp;

    await next(context, ct);
}
```

## Middleware Order

Use `[MiddlewareOrder]` to control execution. Higher values execute first:

```csharp
[MiddlewareOrder(100)]  // Executes first (outermost)
public class ExceptionMiddleware<TKey, TMessage> : IConsumeMiddleware<TKey, TMessage>

[MiddlewareOrder(50)]   // Executes second
public class TracingMiddleware<TKey, TMessage> : IConsumeMiddleware<TKey, TMessage>

[MiddlewareOrder(1)]    // Executes last (closest to handler)
public class LoggingMiddleware<TKey, TMessage> : IConsumeMiddleware<TKey, TMessage>
```

Execution flow:

```
Request:  Exception → Tracing → Logging → Handler
Response: Exception ← Tracing ← Logging ← Handler
```

## Common Middleware Examples

### Exception Handling Middleware

```csharp
[MiddlewareOrder(100)]
public class ExceptionMiddleware<TKey, TMessage>(
    ILogger<ExceptionMiddleware<TKey, TMessage>> logger)
    : IConsumeMiddleware<TKey, TMessage>
{
    public async Task InvokeAsync(
        ConsumeContext<TKey, TMessage> context,
        ConsumeDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        try
        {
            await next(context, ct);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex,
                "Validation failed for message at {Topic}:{Partition}:{Offset}",
                context.Topic, context.Partition, context.Offset);
            // Don't rethrow - skip invalid messages
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error processing message at {Topic}:{Partition}:{Offset}",
                context.Topic, context.Partition, context.Offset);
            throw;
        }
    }
}
```

### Tracing Middleware

```csharp
[MiddlewareOrder(90)]
public class TracingMiddleware<TKey, TMessage>
    : IConsumeMiddleware<TKey, TMessage>
{
    public async Task InvokeAsync(
        ConsumeContext<TKey, TMessage> context,
        ConsumeDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        var parentId = context.Headers.GetString("trace-id");

        using var activity = new Activity("ConsumeMessage")
            .SetParentId(parentId)
            .SetTag("messaging.system", "kafka")
            .SetTag("messaging.destination", context.Topic)
            .SetTag("messaging.kafka.partition", context.Partition)
            .SetTag("messaging.kafka.offset", context.Offset)
            .Start();

        await next(context, ct);
    }
}
```

### Metrics Middleware

```csharp
[MiddlewareOrder(80)]
public class MetricsMiddleware<TKey, TMessage>(
    IMetricsCollector metrics)
    : IConsumeMiddleware<TKey, TMessage>
{
    public async Task InvokeAsync(
        ConsumeContext<TKey, TMessage> context,
        ConsumeDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await next(context, ct);
            metrics.RecordConsumeSuccess(context.Topic, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            metrics.RecordConsumeFailure(context.Topic, ex.GetType().Name);
            throw;
        }
        finally
        {
            // Record lag
            var lag = DateTime.UtcNow - context.Timestamp;
            metrics.RecordConsumeLag(context.Topic, lag.TotalMilliseconds);
        }
    }
}
```

### Idempotency Middleware

```csharp
[MiddlewareOrder(70)]
public class IdempotencyMiddleware<TKey, TMessage>(
    IIdempotencyStore store,
    ILogger<IdempotencyMiddleware<TKey, TMessage>> logger)
    : IConsumeMiddleware<TKey, TMessage>
{
    public async Task InvokeAsync(
        ConsumeContext<TKey, TMessage> context,
        ConsumeDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        var messageId = context.Headers.GetString("message-id")
            ?? $"{context.Topic}-{context.Partition}-{context.Offset}";

        if (await store.ExistsAsync(messageId, ct))
        {
            logger.LogWarning("Duplicate message detected: {MessageId}", messageId);
            return;  // Skip duplicate
        }

        await next(context, ct);

        await store.AddAsync(messageId, ct);
    }
}
```

### Scope Middleware

Create a DI scope per message:

```csharp
[MiddlewareOrder(60)]
public class ScopeMiddleware<TKey, TMessage>(
    IServiceScopeFactory scopeFactory)
    : IConsumeMiddleware<TKey, TMessage>
{
    public async Task InvokeAsync(
        ConsumeContext<TKey, TMessage> context,
        ConsumeDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        // Set scoped values
        var contextAccessor = scope.ServiceProvider.GetService<IMessageContextAccessor>();
        contextAccessor?.SetContext(context);

        await next(context, ct);
    }
}
```

## Registration

Middleware is automatically discovered when calling `AddKafkaBusConsumers`:

```csharp
builder.Services.AddKafkaBusConsumers(
    builder.Configuration,
    [typeof(Program).Assembly]  // Scans for middleware
);
```

## Next Steps

- [Custom Serialization](../serialization/custom) - Implement custom deserializers
- [Advanced Configuration](../advanced/configuration-sections) - Custom configuration sections
