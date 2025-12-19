---
sidebar_position: 5
---

# Configuration

Configure producer behavior through appsettings or custom configuration classes.

## Default Configuration

Add default settings in `appsettings.json`:

```json
{
  "KafkaBus": {
    "DefaultProducerSettings": {
      "BootstrapServers": "localhost:9092",
      "Acks": "All",
      "EnableIdempotence": true,
      "MaxInFlight": 5,
      "MessageTimeoutMs": 30000,
      "RequestTimeoutMs": 30000,
      "RetryBackoffMs": 100,
      "LingerMs": 5
    }
  }
}
```

## Common Settings

| Setting             | Description                                 | Default  |
| ------------------- | ------------------------------------------- | -------- |
| `BootstrapServers`  | Kafka broker addresses                      | Required |
| `Acks`              | Acknowledgment level (None, Leader, All)    | All      |
| `EnableIdempotence` | Enable exactly-once semantics               | false    |
| `MaxInFlight`       | Max unacknowledged requests                 | 5        |
| `MessageTimeoutMs`  | Message delivery timeout                    | 300000   |
| `LingerMs`          | Batching delay                              | 5        |
| `BatchSize`         | Max batch size in bytes                     | 1000000  |
| `CompressionType`   | Compression (None, Gzip, Snappy, Lz4, Zstd) | None     |

## Custom Producer Configuration

Create a configuration class for specific message types:

```csharp
public class OrderProducerConfiguration(IServiceProvider sp)
    : ProducerConfiguration<string, OrderCreated>(sp)
{
    public override string GetKey(OrderCreated message)
        => message.Id.ToString();

    public override Task<ProducerConfig> ConfigureAsync()
    {
        var config = defaultConfiguration;

        // High reliability settings
        config.Acks = Acks.All;
        config.EnableIdempotence = true;
        config.MaxInFlight = 5;

        // Performance tuning
        config.LingerMs = 10;
        config.BatchSize = 500000;
        config.CompressionType = CompressionType.Lz4;

        return Task.FromResult(config);
    }
}
```

## Configuration Profiles

### High Throughput

Optimize for maximum messages per second:

```csharp
public override Task<ProducerConfig> ConfigureAsync()
{
    var config = defaultConfiguration;
    config.Acks = Acks.Leader;           // Don't wait for all replicas
    config.LingerMs = 50;                 // Batch more messages
    config.BatchSize = 1000000;           // Larger batches
    config.CompressionType = CompressionType.Lz4;  // Fast compression
    config.EnableIdempotence = false;     // Skip idempotence overhead
    return Task.FromResult(config);
}
```

### High Reliability

Ensure no message loss:

```csharp
public override Task<ProducerConfig> ConfigureAsync()
{
    var config = defaultConfiguration;
    config.Acks = Acks.All;              // Wait for all replicas
    config.EnableIdempotence = true;      // Exactly-once semantics
    config.MaxInFlight = 5;               // Required for idempotence
    config.MessageTimeoutMs = 60000;      // Longer timeout
    config.RetryBackoffMs = 200;          // More retry delay
    return Task.FromResult(config);
}
```

### Low Latency

Minimize end-to-end latency:

```csharp
public override Task<ProducerConfig> ConfigureAsync()
{
    var config = defaultConfiguration;
    config.Acks = Acks.Leader;           // Faster acks
    config.LingerMs = 0;                  // No batching delay
    config.SocketNagleDisable = true;     // Disable Nagle's algorithm
    return Task.FromResult(config);
}
```

## Custom Serializers

Specify custom serializers in configuration:

```csharp
public class OrderProducerConfiguration(IServiceProvider sp)
    : ProducerConfiguration<Guid, OrderCreated>(sp)
{
    public override Guid GetKey(OrderCreated message) => message.Id;

    public override ISerializer<Guid>? KeySerializer
        => new GuidSerializer();

    public override ISerializer<OrderCreated>? ValueSerializer
        => new JsonSerializer<OrderCreated>();
}
```

## Environment-Based Configuration

Use different settings per environment:

```json
// appsettings.Development.json
{
  "KafkaBus": {
    "DefaultProducerSettings": {
      "BootstrapServers": "localhost:9092"
    }
  }
}

// appsettings.Production.json
{
  "KafkaBus": {
    "DefaultProducerSettings": {
      "BootstrapServers": "kafka-1:9092,kafka-2:9092,kafka-3:9092",
      "Acks": "All",
      "EnableIdempotence": true,
      "SecurityProtocol": "SaslSsl",
      "SaslMechanism": "Plain",
      "SaslUsername": "your-username",
      "SaslPassword": "your-password"
    }
  }
}
```

## Next Steps

- [Middleware](middleware) - Add cross-cutting concerns
- [Custom Serialization](../serialization/custom) - Implement custom serializers
