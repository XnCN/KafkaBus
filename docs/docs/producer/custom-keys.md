---
sidebar_position: 3
---

# Custom Keys

Message keys determine partition assignment and enable ordering guarantees.

## Why Use Keys?

- **Partitioning**: Messages with the same key go to the same partition
- **Ordering**: Messages in the same partition are processed in order
- **Compaction**: Log compaction keeps only the latest value per key

## Using Different Key Types

### String Keys (Default)

```csharp
await messageBus.SendAsync("orders", message);
```

### Guid Keys

```csharp
await messageBus.SendAsync<Guid, OrderCreated>("orders", message);
```

### Integer Keys

```csharp
await messageBus.SendAsync<int, UserEvent>("users", message);
```

### Long Keys

```csharp
await messageBus.SendAsync<long, Transaction>("transactions", message);
```

## Custom Key Configuration

Create a producer configuration to control how keys are generated:

```csharp
public class OrderProducerConfiguration(IServiceProvider sp)
    : ProducerConfiguration<string, OrderCreated>(sp)
{
    public override string GetKey(OrderCreated message)
    {
        // Use order ID as key - ensures all events for an order
        // go to the same partition
        return message.Id.ToString();
    }
}
```

### Guid Key with Custom Serializer

```csharp
public class OrderProducerConfiguration(IServiceProvider sp)
    : ProducerConfiguration<Guid, OrderCreated>(sp)
{
    public override Guid GetKey(OrderCreated message) => message.Id;

    public override ISerializer<Guid>? KeySerializer => new GuidSerializer();
}

public class GuidSerializer : ISerializer<Guid>
{
    public byte[] Serialize(Guid data, SerializationContext context)
    {
        return data.ToByteArray();
    }
}
```

### Composite Keys

For complex partitioning strategies:

```csharp
public class OrderProducerConfiguration(IServiceProvider sp)
    : ProducerConfiguration<string, OrderCreated>(sp)
{
    public override string GetKey(OrderCreated message)
    {
        // Composite key: region + customer
        return $"{message.Region}:{message.CustomerId}";
    }
}
```

## Key Strategies

### By Entity ID

Best for event sourcing and ensuring entity events are ordered:

```csharp
public override string GetKey(OrderEvent message) => message.OrderId.ToString();
```

### By Tenant

Best for multi-tenant systems:

```csharp
public override string GetKey(TenantEvent message) => message.TenantId;
```

### By User

Best for user activity tracking:

```csharp
public override string GetKey(UserActivity message) => message.UserId.ToString();
```

### Round Robin (No Key)

Return null or empty for round-robin distribution:

```csharp
public override string GetKey(LogEntry message) => string.Empty;
```

:::warning
Without keys, message ordering is not guaranteed across partitions.
:::

## Complete Example

```csharp
// Message
public record PaymentProcessed(
    Guid PaymentId,
    Guid OrderId,
    string CustomerId,
    decimal Amount
);

// Configuration
public class PaymentProducerConfiguration(IServiceProvider sp)
    : ProducerConfiguration<string, PaymentProcessed>(sp)
{
    public override string GetKey(PaymentProcessed message)
    {
        // Key by order ID - all payment events for an order stay together
        return message.OrderId.ToString();
    }

    public override Task<ProducerConfig> ConfigureAsync()
    {
        var config = defaultConfiguration;
        config.Acks = Acks.All;
        return Task.FromResult(config);
    }
}

// Usage
await messageBus.SendAsync("payments", new PaymentProcessed(
    Guid.NewGuid(),
    orderId,
    customerId,
    99.99m
));
```

## Next Steps

- [Headers](headers) - Add metadata to messages
- [Configuration](configuration) - Advanced producer settings
