---
sidebar_position: 4
---

# Message Metadata

Access message metadata through `ConsumeContext`.

## Available Properties

```csharp
public override Task HandleAsync(
    ConsumeContext<string, OrderCreated> context,
    CancellationToken ct)
{
    // Message key
    var key = context.Key;

    // Deserialized message
    var message = context.Message;

    // Topic information
    var topic = context.Topic;
    var partition = context.Partition;
    var offset = context.Offset;

    // Timestamp when message was produced
    var timestamp = context.Timestamp;

    // Message headers
    var headers = context.Headers;

    return Task.CompletedTask;
}
```

## Offset Tracking

Use offset information for deduplication or progress tracking:

```csharp
public class OrderConsumer(
    IOffsetTracker tracker,
    ILogger<OrderConsumer> logger)
    : MessageConsumer<OrderCreated>
{
    public override async Task HandleAsync(
        ConsumeContext<string, OrderCreated> context,
        CancellationToken ct)
    {
        // Check if already processed (deduplication)
        if (await tracker.IsProcessedAsync(context.Topic, context.Partition, context.Offset))
        {
            logger.LogWarning("Message already processed, skipping");
            return;
        }

        await ProcessOrderAsync(context.Message, ct);

        // Track progress
        await tracker.MarkProcessedAsync(context.Topic, context.Partition, context.Offset);
    }
}
```

## Timestamp Usage

Use message timestamp for time-based logic:

```csharp
public override Task HandleAsync(
    ConsumeContext<string, OrderCreated> context,
    CancellationToken ct)
{
    var messageAge = DateTime.UtcNow - context.Timestamp;

    if (messageAge > TimeSpan.FromHours(24))
    {
        logger.LogWarning(
            "Stale message detected. Age: {Age}, Produced: {Timestamp}",
            messageAge,
            context.Timestamp
        );
    }

    // Calculate processing lag
    logger.LogInformation(
        "Processing message from {Timestamp}, lag: {LagMs}ms",
        context.Timestamp,
        messageAge.TotalMilliseconds
    );

    return Task.CompletedTask;
}
```

## Next Steps

- [Middleware](middleware) - Add cross-cutting concerns
- [Custom Serialization](../serialization/custom) - Implement custom deserializers
