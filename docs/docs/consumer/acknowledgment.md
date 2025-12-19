---
sidebar_position: 3
---

# Acknowledgment

Control when messages are marked as processed using manual acknowledgment.

## Auto vs Manual Acknowledgment

| Mode           | Use Case                                    | Configuration             |
| -------------- | ------------------------------------------- | ------------------------- |
| Auto (default) | Simple, fire-and-forget processing          | `EnableAutoCommit: true`  |
| Manual         | Reliable processing, exactly-once semantics | `EnableAutoCommit: false` |

## Enabling Manual Acknowledgment

Disable auto-commit in `appsettings.json`:

```json
{
  "KafkaBus": {
    "DefaultConsumerSettings": {
      "BootstrapServers": "localhost:9092",
      "EnableAutoCommit": false
    }
  }
}
```

## Single Message Acknowledgment

Call `Ack(context)` after successful processing:

```csharp
public class OrderConsumer(ILogger<OrderConsumer> logger)
    : MessageConsumer<OrderCreated>
{
    public override async Task HandleAsync(
        ConsumeContext<string, OrderCreated> context,
        CancellationToken ct)
    {
        try
        {
            await ProcessOrderAsync(context.Message, ct);

            // Acknowledge only after successful processing
            Ack(context);

            logger.LogInformation("Order {Id} processed and acknowledged", context.Message.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process order {Id}", context.Message.Id);
            // Don't call Ack() - message will be redelivered
            throw;
        }
    }
}
```

## Batch Acknowledgment

Acknowledge multiple messages at once for better performance:

```csharp
public class BatchOrderConsumer(ILogger<BatchOrderConsumer> logger)
    : MessageConsumer<OrderCreated>
{
    private readonly List<ConsumeContext<string, OrderCreated>> _batch = [];
    private const int BatchSize = 100;

    public override async Task HandleAsync(
        ConsumeContext<string, OrderCreated> context,
        CancellationToken ct)
    {
        _batch.Add(context);

        if (_batch.Count >= BatchSize)
        {
            await ProcessBatchAsync(_batch.Select(c => c.Message), ct);

            // Acknowledge all messages in the batch
            Ack(_batch);

            logger.LogInformation("Processed and acknowledged {Count} orders", _batch.Count);
            _batch.Clear();
        }
    }

    private async Task ProcessBatchAsync(IEnumerable<OrderCreated> orders, CancellationToken ct)
    {
        // Batch insert to database, etc.
    }
}
```

## Conditional Acknowledgment

Acknowledge based on processing result:

```csharp
public override async Task HandleAsync(
    ConsumeContext<string, OrderCreated> context,
    CancellationToken ct)
{
    var result = await ProcessOrderAsync(context.Message, ct);

    switch (result)
    {
        case ProcessResult.Success:
            Ack(context);
            logger.LogInformation("Order processed successfully");
            break;

        case ProcessResult.Skip:
            // Acknowledge to skip invalid messages
            Ack(context);
            logger.LogWarning("Order skipped due to validation");
            break;

        case ProcessResult.Retry:
            // Don't acknowledge - will be redelivered
            logger.LogWarning("Order will be retried");
            throw new RetryableException();
    }
}
```

## Dead Letter Pattern

Move failed messages to a dead letter topic:

```csharp
public class OrderConsumer(
    IMessageBus messageBus,
    ILogger<OrderConsumer> logger)
    : MessageConsumer<OrderCreated>
{
    private const int MaxRetries = 3;

    public override async Task HandleAsync(
        ConsumeContext<string, OrderCreated> context,
        CancellationToken ct)
    {
        var retryCount = GetRetryCount(context.Headers);

        try
        {
            await ProcessOrderAsync(context.Message, ct);
            Ack(context);
        }
        catch (Exception ex)
        {
            if (retryCount >= MaxRetries)
            {
                // Send to dead letter topic
                var headers = new Headers
                {
                    { "error", Encoding.UTF8.GetBytes(ex.Message) },
                    { "original-topic", Encoding.UTF8.GetBytes(context.Topic) }
                };

                await messageBus.SendAsync("orders-dlq", context.Message, headers, ct);
                Ack(context);  // Acknowledge original message

                logger.LogError(ex, "Order {Id} sent to DLQ after {Retries} retries",
                    context.Message.Id, MaxRetries);
            }
            else
            {
                throw;  // Don't ack - will be retried
            }
        }
    }

    private int GetRetryCount(Headers? headers)
    {
        if (headers?.TryGetLastBytes("retry-count", out var bytes) == true)
            return int.Parse(Encoding.UTF8.GetString(bytes));
        return 0;
    }
}
```

## Acknowledgment with Transactions

Combine with database transactions:

```csharp
public class OrderConsumer(
    IDbContextFactory<AppDbContext> dbFactory,
    ILogger<OrderConsumer> logger)
    : MessageConsumer<OrderCreated>
{
    public override async Task HandleAsync(
        ConsumeContext<string, OrderCreated> context,
        CancellationToken ct)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        try
        {
            var order = new Order
            {
                Id = context.Message.Id,
                ProductName = context.Message.ProductName,
                Amount = context.Message.Amount
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            // Only acknowledge after successful commit
            Ack(context);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
```

## Best Practices

:::tip Do

- Always acknowledge after successful processing
- Use batch acknowledgment for high-throughput scenarios
- Implement dead letter queues for unprocessable messages
- Combine with database transactions for consistency
  :::

:::warning Don't

- Don't acknowledge before processing completes
- Don't swallow exceptions without acknowledging
- Don't use auto-commit for critical business operations
  :::

## Next Steps

- [Metadata](metadata) - Access message metadata
- [Middleware](middleware) - Add cross-cutting concerns
