---
sidebar_position: 1
---

# Basic Usage

Learn how to send messages to Kafka using KafkaBus.

## IMessageBus Interface

The `IMessageBus` interface is your primary way to send messages. Inject it into your services:

```csharp
public class OrderService(IMessageBus messageBus)
{
    public async Task CreateOrderAsync(Order order)
    {
        await messageBus.SendAsync("orders", order);
    }
}
```

## Send Methods

### Send to Topic

Send a message to a topic with automatic partition assignment:

```csharp
// String key (default)
await messageBus.SendAsync("orders", new OrderCreated(Guid.NewGuid(), "Product", 99.99m));

// With custom key type
await messageBus.SendAsync<Guid, OrderCreated>("orders", message);
```

### Send to Specific Partition

When you need control over which partition receives the message:

```csharp
// String key
await messageBus.SendAsync("orders", partition: 2, message);

// Custom key type
await messageBus.SendAsync<Guid, OrderCreated>("orders", partition: 2, message);
```

### Send Batch

Send multiple messages efficiently:

```csharp
var orders = new[]
{
    new OrderCreated(Guid.NewGuid(), "Product A", 10.00m),
    new OrderCreated(Guid.NewGuid(), "Product B", 20.00m),
    new OrderCreated(Guid.NewGuid(), "Product C", 30.00m)
};

// String key
var results = await messageBus.SendBatchAsync("orders", orders);

// Custom key type
var results = await messageBus.SendBatchAsync<Guid, OrderCreated>("orders", orders);
```

## Key Types

KafkaBus supports any key type. The default is `string`:

```csharp
// Default string key
await messageBus.SendAsync("topic", message);

// Guid key
await messageBus.SendAsync<Guid, OrderCreated>("topic", message);

// Int key
await messageBus.SendAsync<int, UserEvent>("topic", message);

// Long key
await messageBus.SendAsync<long, Transaction>("topic", message);
```

:::tip
Use meaningful keys for partitioning. Messages with the same key always go to the same partition, ensuring ordering.
:::

## Complete Example

```csharp
public class NotificationService(IMessageBus messageBus, ILogger<NotificationService> logger)
{
    public async Task SendOrderNotificationAsync(Order order)
    {
        var notification = new OrderNotification
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        };

        var result = await messageBus.SendAsync("notifications", notification);

        logger.LogInformation(
            "Notification sent to partition {Partition} at offset {Offset}",
            result.Partition.Value,
            result.Offset.Value
        );
    }

    public async Task SendBulkNotificationsAsync(IEnumerable<Order> orders)
    {
        var notifications = orders.Select(o => new OrderNotification
        {
            OrderId = o.Id,
            CustomerEmail = o.CustomerEmail,
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        });

        var results = await messageBus.SendBatchAsync("notifications", notifications);

        logger.LogInformation("Sent {Count} notifications", results.Count());
    }
}
```

## Next Steps

- [Delivery Result](delivery-result) - Handle send results
- [Custom Keys](custom-keys) - Configure message keys
- [Headers](headers) - Add metadata to messages
