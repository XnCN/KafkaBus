---
sidebar_position: 6
---

# Producer Middleware

Middleware allows you to intercept messages before they're sent to Kafka.

## Creating Middleware

Implement `IProduceMiddleware<TKey, TMessage>`:

```csharp
[MiddlewareOrder(1)]
public class LoggingMiddleware<TKey, TMessage>(
    ILogger<LoggingMiddleware<TKey, TMessage>> logger)
    : IProduceMiddleware<TKey, TMessage>
{
    public async Task<DeliveryResult<TKey, TMessage>> InvokeAsync(
        ProduceContext<TKey, TMessage> context,
        ProduceDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Producing to {Topic}", context.Topic);

        var result = await next(context, ct);

        logger.LogInformation(
            "Produced to {Topic} P:{Partition} O:{Offset} in {Elapsed}ms",
            context.Topic,
            result.Partition.Value,
            result.Offset.Value,
            sw.ElapsedMilliseconds
        );

        return result;
    }
}
```

## ProduceContext

The context provides access to message details:

```csharp
public async Task<DeliveryResult<TKey, TMessage>> InvokeAsync(
    ProduceContext<TKey, TMessage> context,
    ProduceDelegate<TKey, TMessage> next,
    CancellationToken ct)
{
    // Access context properties
    var topic = context.Topic;
    var partition = context.Partition;     // null if not specified
    var key = context.Key;
    var message = context.Message;
    var headers = context.Headers;

    return await next(context, ct);
}
```

## Middleware Order

Use `[MiddlewareOrder]` to control execution order. Higher values execute first:

```csharp
[MiddlewareOrder(100)]  // Executes first (outermost)
public class AuthMiddleware<TKey, TMessage> : IProduceMiddleware<TKey, TMessage>

[MiddlewareOrder(50)]   // Executes second
public class ValidationMiddleware<TKey, TMessage> : IProduceMiddleware<TKey, TMessage>

[MiddlewareOrder(1)]    // Executes last (closest to Kafka)
public class LoggingMiddleware<TKey, TMessage> : IProduceMiddleware<TKey, TMessage>
```

Execution flow:

```
Request:  Auth → Validation → Logging → Kafka
Response: Auth ← Validation ← Logging ← Kafka
```

## Common Middleware Examples

### Correlation ID Middleware

Automatically add correlation headers:

```csharp
[MiddlewareOrder(90)]
public class CorrelationMiddleware<TKey, TMessage>
    : IProduceMiddleware<TKey, TMessage>
{
    public async Task<DeliveryResult<TKey, TMessage>> InvokeAsync(
        ProduceContext<TKey, TMessage> context,
        ProduceDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        context.Headers ??= new Headers();

        if (!context.Headers.Any(h => h.Key == "correlation-id"))
        {
            var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            context.Headers.Add("correlation-id", Encoding.UTF8.GetBytes(correlationId));
        }

        return await next(context, ct);
    }
}
```

### Metrics Middleware

Track production metrics:

```csharp
[MiddlewareOrder(80)]
public class MetricsMiddleware<TKey, TMessage>(
    IMetricsCollector metrics)
    : IProduceMiddleware<TKey, TMessage>
{
    public async Task<DeliveryResult<TKey, TMessage>> InvokeAsync(
        ProduceContext<TKey, TMessage> context,
        ProduceDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var result = await next(context, ct);

            metrics.RecordProduceSuccess(context.Topic, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            metrics.RecordProduceFailure(context.Topic, ex.GetType().Name);
            throw;
        }
    }
}
```

### Validation Middleware

Validate messages before sending:

```csharp
[MiddlewareOrder(70)]
public class ValidationMiddleware<TKey, TMessage>(
    IValidator<TMessage> validator)
    : IProduceMiddleware<TKey, TMessage>
{
    public async Task<DeliveryResult<TKey, TMessage>> InvokeAsync(
        ProduceContext<TKey, TMessage> context,
        ProduceDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(context.Message, ct);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        return await next(context, ct);
    }
}
```

### Enrichment Middleware

Add standard headers to all messages:

```csharp
[MiddlewareOrder(60)]
public class EnrichmentMiddleware<TKey, TMessage>
    : IProduceMiddleware<TKey, TMessage>
{
    public async Task<DeliveryResult<TKey, TMessage>> InvokeAsync(
        ProduceContext<TKey, TMessage> context,
        ProduceDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        context.Headers ??= new Headers();
        context.Headers.Add("produced-at", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")));
        context.Headers.Add("message-type", Encoding.UTF8.GetBytes(typeof(TMessage).Name));
        context.Headers.Add("service", Encoding.UTF8.GetBytes("order-service"));

        return await next(context, ct);
    }
}
```

## Registration

Middleware is automatically discovered and registered when you call `AddKafkaBusProducers`:

```csharp
builder.Services.AddKafkaBusProducers(
    builder.Configuration,
    [typeof(Program).Assembly]  // Scans this assembly for middleware
);
```

## Next Steps

- [Consumer Middleware](../consumer/middleware) - Intercept consumed messages
- [Custom Serialization](../serialization/custom) - Implement custom serializers
