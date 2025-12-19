---
sidebar_position: 2
---

# Delivery Result

Every send operation returns a `DeliveryResult` containing metadata about the produced message.

## Result Properties

```csharp
var result = await messageBus.SendAsync("orders", message);

// Topic and partition info
Console.WriteLine($"Topic: {result.Topic}");
Console.WriteLine($"Partition: {result.Partition.Value}");
Console.WriteLine($"Offset: {result.Offset.Value}");

// Timestamp
Console.WriteLine($"Timestamp: {result.Timestamp.UtcDateTime}");

// Key used for the message
Console.WriteLine($"Key: {result.Key}");

// Delivery status
Console.WriteLine($"Status: {result.Status}");

// Access the original message
Console.WriteLine($"Message: {result.Message.Value}");
Console.WriteLine($"Headers: {result.Message.Headers}");
```

## Delivery Status

The `Status` property indicates whether the message was persisted:

| Status              | Description                               |
| ------------------- | ----------------------------------------- |
| `Persisted`         | Message was successfully written to Kafka |
| `PossiblyPersisted` | Message may or may not have been written  |
| `NotPersisted`      | Message was not written to Kafka          |

```csharp
var result = await messageBus.SendAsync("orders", message);

switch (result.Status)
{
    case PersistenceStatus.Persisted:
        logger.LogInformation("Message delivered successfully");
        break;
    case PersistenceStatus.PossiblyPersisted:
        logger.LogWarning("Message delivery uncertain");
        break;
    case PersistenceStatus.NotPersisted:
        logger.LogError("Message delivery failed");
        break;
}
```

## Batch Results

When sending batches, you receive a collection of results:

```csharp
var messages = new[] { message1, message2, message3 };
var results = await messageBus.SendBatchAsync("orders", messages);

foreach (var result in results)
{
    if (result.Status == PersistenceStatus.Persisted)
    {
        logger.LogInformation(
            "Delivered to P:{Partition} O:{Offset}",
            result.Partition.Value,
            result.Offset.Value
        );
    }
    else
    {
        logger.LogWarning(
            "Delivery uncertain: {Status}",
            result.Status
        );
    }
}

// Summary
var succeeded = results.Count(r => r.Status == PersistenceStatus.Persisted);
var total = results.Count();
logger.LogInformation("Delivered {Succeeded}/{Total} messages", succeeded, total);
```

## Extracting Message ID

Use the offset as a unique message identifier:

```csharp
public class OrderService(IMessageBus messageBus)
{
    public async Task<long> CreateOrderAsync(Order order)
    {
        var result = await messageBus.SendAsync("orders", order);

        // Return offset as message ID
        return result.Offset.Value;
    }
}
```

## Error Handling

Handle delivery failures gracefully:

```csharp
public async Task<bool> TrySendAsync<T>(string topic, T message)
{
    try
    {
        var result = await messageBus.SendAsync(topic, message);

        if (result.Status != PersistenceStatus.Persisted)
        {
            logger.LogWarning(
                "Message to {Topic} may not be persisted: {Status}",
                topic,
                result.Status
            );
            return false;
        }

        return true;
    }
    catch (ProduceException<string, T> ex)
    {
        logger.LogError(ex, "Failed to produce message to {Topic}", topic);
        return false;
    }
}
```

## Next Steps

- [Custom Keys](custom-keys) - Configure message keys
- [Headers](headers) - Add metadata to messages
