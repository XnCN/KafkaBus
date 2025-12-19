---
sidebar_position: 2
---

# Configuration

Configure consumer behavior through appsettings or custom configuration classes.

## Default Configuration

Add default settings in `appsettings.json`:

```json
{
  "KafkaBus": {
    "DefaultConsumerSettings": {
      "BootstrapServers": "localhost:9092",
      "AutoOffsetReset": "Earliest",
      "EnableAutoCommit": true,
      "AutoCommitIntervalMs": 5000,
      "SessionTimeoutMs": 45000,
      "MaxPollIntervalMs": 300000
    }
  }
}
```

## Common Settings

| Setting                | Description                       | Default  |
| ---------------------- | --------------------------------- | -------- |
| `BootstrapServers`     | Kafka broker addresses            | Required |
| `AutoOffsetReset`      | Where to start (Earliest, Latest) | Latest   |
| `EnableAutoCommit`     | Auto-acknowledge messages         | true     |
| `AutoCommitIntervalMs` | Auto-commit frequency             | 5000     |
| `SessionTimeoutMs`     | Consumer session timeout          | 45000    |
| `MaxPollIntervalMs`    | Max time between polls            | 300000   |
| `FetchMinBytes`        | Minimum fetch size                | 1        |
| `FetchMaxBytes`        | Maximum fetch size                | 52428800 |

## Custom Consumer Configuration

Create a configuration class for specific message types:

```csharp
public class OrderConsumerConfiguration(IServiceProvider sp)
    : ConsumerConfiguration<string, OrderCreated>(sp)
{
    public override string Topic => "orders";
    public override string GroupId => "order-processing-service";
    public override int WorkerCount => 4;
}
```

## Configuration Properties

### Topic

The Kafka topic to consume from:

```csharp
public override string Topic => "orders";
```

### GroupId

Consumer group for load balancing:

```csharp
public override string GroupId => "order-service";
```

:::tip
Consumers in the same group share partitions. Different groups receive all messages independently.
:::

### WorkerCount

Number of parallel workers (threads):

```csharp
public override int WorkerCount => 4;
```

:::note
`WorkerCount` should not exceed the number of partitions. Extra workers will be idle.
:::

## Advanced Configuration

Override `ConfigureAsync` for full control:

```csharp
public class OrderConsumerConfiguration(IServiceProvider sp)
    : ConsumerConfiguration<string, OrderCreated>(sp)
{
    public override string Topic => "orders";
    public override string GroupId => "order-service";
    public override int WorkerCount => 4;

    public override Task<ConsumerConfig> ConfigureAsync()
    {
        var config = defaultConfiguration;

        config.GroupId = GroupId;
        config.AutoOffsetReset = AutoOffsetReset.Earliest;
        config.EnableAutoCommit = false;  // Manual ack
        config.SessionTimeoutMs = 30000;
        config.MaxPollIntervalMs = 600000;  // 10 minutes for long processing
        config.FetchMinBytes = 1024;
        config.FetchMaxBytes = 10485760;  // 10MB

        return Task.FromResult(config);
    }
}
```

## Configuration Profiles

### High Throughput

Process maximum messages per second:

```csharp
public override Task<ConsumerConfig> ConfigureAsync()
{
    var config = defaultConfiguration;
    config.GroupId = GroupId;
    config.EnableAutoCommit = true;
    config.AutoCommitIntervalMs = 1000;
    config.FetchMinBytes = 10240;      // Wait for 10KB
    config.FetchMaxBytes = 52428800;   // 50MB max
    config.MaxPartitionFetchBytes = 10485760;  // 10MB per partition
    return Task.FromResult(config);
}
```

### Reliable Processing

Ensure no message loss:

```csharp
public override Task<ConsumerConfig> ConfigureAsync()
{
    var config = defaultConfiguration;
    config.GroupId = GroupId;
    config.EnableAutoCommit = false;   // Manual acknowledgment
    config.AutoOffsetReset = AutoOffsetReset.Earliest;
    config.EnableAutoOffsetStore = false;
    config.IsolationLevel = IsolationLevel.ReadCommitted;
    return Task.FromResult(config);
}
```

### Long Processing

For handlers that take time:

```csharp
public override Task<ConsumerConfig> ConfigureAsync()
{
    var config = defaultConfiguration;
    config.GroupId = GroupId;
    config.MaxPollIntervalMs = 1800000;  // 30 minutes
    config.SessionTimeoutMs = 60000;      // 1 minute
    config.HeartbeatIntervalMs = 20000;   // 20 seconds
    return Task.FromResult(config);
}
```

## Custom Deserializers

Specify custom deserializers:

```csharp
public class OrderConsumerConfiguration(IServiceProvider sp)
    : ConsumerConfiguration<Guid, OrderCreated>(sp)
{
    public override string Topic => "orders";

    public override IDeserializer<Guid>? KeyDeserializer
        => new GuidDeserializer();

    public override IDeserializer<OrderCreated>? ValueDeserializer
        => new JsonDeserializer<OrderCreated>();
}
```

## Environment-Based Configuration

```json
// appsettings.Development.json
{
  "KafkaBus": {
    "DefaultConsumerSettings": {
      "BootstrapServers": "localhost:9092",
      "AutoOffsetReset": "Earliest"
    }
  }
}

// appsettings.Production.json
{
  "KafkaBus": {
    "DefaultConsumerSettings": {
      "BootstrapServers": "kafka-1:9092,kafka-2:9092",
      "AutoOffsetReset": "Latest",
      "SecurityProtocol": "SaslSsl",
      "SaslMechanism": "Plain",
      "SaslUsername": "your-username",
      "SaslPassword": "your-password"
    }
  }
}
```

## Next Steps

- [Acknowledgment](acknowledgment) - Manual message acknowledgment
- [Metadata](metadata) - Access message metadata
- [Middleware](middleware) - Add cross-cutting concerns
