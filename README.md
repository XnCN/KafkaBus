# KafkaBus

A lightweight, middleware-enabled Kafka library for .NET that simplifies producer and consumer implementations with a clean, fluent API.

[![NuGet](https://img.shields.io/nuget/v/KafkaBus.svg)](https://www.nuget.org/packages/KafkaBus)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Features

- 🚀 Simple and intuitive API
- 🔌 Built-in dependency injection support
- 🔗 Middleware pipeline for producers and consumers
- ⚡ Batch message production
- 🔄 Manual acknowledgment support
- 🎯 Custom serialization/deserialization
- 📦 Convention-based configuration scanning

## Installation

```bash
dotnet add package KafkaBus
```

## Quick Start

### 1. Configure Services

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKafkaBusProducers(builder.Configuration, [typeof(Program).Assembly]);
builder.Services.AddKafkaBusConsumers(builder.Configuration, [typeof(Program).Assembly]);

var app = builder.Build();
app.Run();
```

### 2. Add Configuration

```json
{
  "KafkaBus": {
    "DefaultProducerSettings": {
      "BootstrapServers": "localhost:9092"
    },
    "DefaultConsumerSettings": {
      "BootstrapServers": "localhost:9092",
      "AutoOffsetReset": "Earliest",
      "EnableAutoCommit": true
    }
  }
}
```

### 3. Define Your Message

```csharp
public record OrderCreated(Guid Id, string ProductName, decimal Amount);
```

### 4. Send Messages

```csharp
public class OrderController(IMessageBus messageBus) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        var message = new OrderCreated(Guid.NewGuid(), request.ProductName, request.Amount);
        
        await messageBus.SendAsync("orders", message);
        
        return Ok();
    }
}
```

### 5. Consume Messages

```csharp
public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) 
    : MessageConsumer<OrderCreated>
{
    public override Task HandleAsync(ConsumeContext<string, OrderCreated> context, CancellationToken ct)
    {
        logger.LogInformation("Order received: {OrderId}", context.Message.Id);
        return Task.CompletedTask;
    }
}

public class OrderCreatedConsumerConfiguration(IServiceProvider sp) 
    : ConsumerConfiguration<string, OrderCreated>(sp)
{
    public override string Topic => "orders";
}
```

That's it! Your consumer will automatically start listening to the `orders` topic.

---

## Producer Features

### Basic Usage

```csharp
// Send to topic (string key)
var result = await messageBus.SendAsync("orders", new OrderCreated(...));

// Send to specific partition
var result = await messageBus.SendAsync("orders", partition: 2, new OrderCreated(...));

// Send batch
var orders = new[] { order1, order2, order3 };
var results = await messageBus.SendBatchAsync("orders", orders);
```

### Delivery Result

The `SendAsync` method returns a `DeliveryResult` containing metadata about the produced message:

```csharp
var result = await messageBus.SendAsync("orders", message);

Console.WriteLine($"Topic: {result.Topic}");
Console.WriteLine($"Partition: {result.Partition.Value}");
Console.WriteLine($"Offset: {result.Offset.Value}");
Console.WriteLine($"Timestamp: {result.Timestamp.UtcDateTime}");
Console.WriteLine($"Key: {result.Key}");
Console.WriteLine($"Status: {result.Status}"); // Persisted, PossiblyPersisted, NotPersisted

// Access the produced message
Console.WriteLine($"Message Value: {result.Message.Value}");
Console.WriteLine($"Message Headers: {result.Message.Headers}");
```

### Batch Result

```csharp
var results = await messageBus.SendBatchAsync("orders", orders);

foreach (var result in results)
{
    if (result.Status == PersistenceStatus.Persisted)
        Console.WriteLine($"Delivered to P:{result.Partition.Value} O:{result.Offset.Value}");
    else
        Console.WriteLine($"Delivery uncertain: {result.Status}");
}
```

### Custom Key Types

```csharp
// Send with Guid key
await messageBus.SendAsync<Guid, OrderCreated>("orders", message);

// Send with int key
await messageBus.SendAsync<int, UserEvent>("users", message);

// Send with custom key to specific partition
await messageBus.SendAsync<Guid, OrderCreated>("orders", partition: 1, message);

// Batch with custom key
await messageBus.SendBatchAsync<Guid, OrderCreated>("orders", messages);
```

### Custom Headers

```csharp
var headers = new Headers
{
    { "correlation-id", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
    { "source", Encoding.UTF8.GetBytes("order-service") }
};

// With string key
await messageBus.SendAsync("orders", message, headers);

// With custom key
await messageBus.SendAsync<Guid, OrderCreated>("orders", message, headers);
```

### Custom Producer Configuration

Create a custom configuration to control keys, serializers, or Kafka settings:

```csharp
public class OrderProducerConfiguration(IServiceProvider sp) 
    : ProducerConfiguration<string, OrderCreated>(sp)
{
    // Use OrderId as the message key for partitioning
    public override string GetKey(OrderCreated message) => message.Id.ToString();
    
    // Custom Kafka settings
    public override Task<ProducerConfig> ConfigureAsync()
    {
        var config = defaultConfiguration;
        config.Acks = Acks.All;
        config.EnableIdempotence = true;
        config.MaxInFlight = 5;
        return Task.FromResult(config);
    }
}
```

### Custom Key Configuration with Serializer

```csharp
public class OrderProducerConfiguration(IServiceProvider sp) 
    : ProducerConfiguration<Guid, OrderCreated>(sp)
{
    public override Guid GetKey(OrderCreated message) => message.Id;
    
    public override ISerializer<Guid>? KeySerializer => new GuidSerializer();
}
```

---

## Consumer Features

### Basic Consumer

```csharp
public class OrderConsumer : MessageConsumer<OrderCreated>
{
    public override Task HandleAsync(ConsumeContext<string, OrderCreated> context, CancellationToken ct)
    {
        Console.WriteLine($"Received: {context.Message.Id}");
        return Task.CompletedTask;
    }
}
```

### Custom Consumer Configuration

```csharp
public class OrderConsumerConfiguration(IServiceProvider sp) 
    : ConsumerConfiguration<string, OrderCreated>(sp)
{
    public override string Topic => "orders";           // Custom topic name
    public override string GroupId => "order-service";  // Custom consumer group
    public override int WorkerCount => 4;               // Parallel workers
}
```

### Manual Acknowledgment

For scenarios requiring manual message acknowledgment, disable auto commit in appsettings:

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

```csharp
public class OrderConsumer(ILogger<OrderConsumer> logger) 
    : MessageConsumer<OrderCreated>
{
    public override async Task HandleAsync(ConsumeContext<string, OrderCreated> context, CancellationToken ct)
    {
        try
        {
            await ProcessOrderAsync(context.Message);
            Ack(context); // Manually acknowledge
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Processing failed, message will be redelivered");
            throw;
        }
    }
}
```

### Batch Acknowledgment

```csharp
public class BatchOrderConsumer : MessageConsumer<OrderCreated>
{
    private readonly List<ConsumeContext<string, OrderCreated>> _batch = [];
    private const int BatchSize = 100;

    public override async Task HandleAsync(ConsumeContext<string, OrderCreated> context, CancellationToken ct)
    {
        _batch.Add(context);

        if (_batch.Count >= BatchSize)
        {
            await ProcessBatchAsync(_batch.Select(c => c.Message));
            Ack(_batch); // Acknowledge all at once
            _batch.Clear();
        }
    }
}
```

### Accessing Message Metadata

```csharp
public override Task HandleAsync(ConsumeContext<string, OrderCreated> context, CancellationToken ct)
{
    Console.WriteLine($"Topic: {context.Topic}");
    Console.WriteLine($"Partition: {context.Partition}");
    Console.WriteLine($"Offset: {context.Offset}");
    Console.WriteLine($"Timestamp: {context.Timestamp}");
    Console.WriteLine($"Key: {context.Key}");
    
    // Access headers
    if (context.Headers?.TryGetLastBytes("correlation-id", out var bytes) == true)
    {
        var correlationId = Encoding.UTF8.GetString(bytes);
    }
    
    return Task.CompletedTask;
}
```

### Custom Key Consumer

```csharp
public class OrderConsumer : MessageConsumer<Guid, OrderCreated>
{
    public override Task HandleAsync(ConsumeContext<Guid, OrderCreated> context, CancellationToken ct)
    {
        Console.WriteLine($"Key: {context.Key}, Order: {context.Message.Id}");
        return Task.CompletedTask;
    }
}

public class OrderConsumerConfiguration(IServiceProvider sp) 
    : ConsumerConfiguration<Guid, OrderCreated>(sp)
{
    public override string Topic => "orders";
    public override IDeserializer<Guid>? KeyDeserializer => new GuidDeserializer();
}
```

---

## Middleware

Middlewares allow you to intercept and process messages in a pipeline pattern.

### Producer Middleware

```csharp
[MiddlewareOrder(1)]
public class ProducerLoggingMiddleware<TKey, TMessage>(ILogger<ProducerLoggingMiddleware<TKey, TMessage>> logger) 
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

        logger.LogInformation("Produced to {Topic} P:{Partition} O:{Offset} in {Elapsed}ms",
            context.Topic, result.Partition.Value, result.Offset.Value, sw.ElapsedMilliseconds);

        return result;
    }
}
```

### Consumer Middleware

```csharp
[MiddlewareOrder(1)]
public class ConsumerLoggingMiddleware<TKey, TMessage>(ILogger<ConsumerLoggingMiddleware<TKey, TMessage>> logger) 
    : IConsumeMiddleware<TKey, TMessage>
{
    public async Task InvokeAsync(
        ConsumeContext<TKey, TMessage> context,
        ConsumeDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Consuming from {Topic} P:{Partition} O:{Offset}",
            context.Topic, context.Partition, context.Offset);

        await next(context, ct);

        logger.LogInformation("Consumed in {Elapsed}ms", sw.ElapsedMilliseconds);
    }
}
```

### Middleware Order

Use `[MiddlewareOrder]` attribute to control execution order. Higher values execute first (outermost in pipeline).

```csharp
[MiddlewareOrder(100)] // Executes first
public class AuthenticationMiddleware<TKey, TMessage> : IConsumeMiddleware<TKey, TMessage> { }

[MiddlewareOrder(50)]  // Executes second
public class ValidationMiddleware<TKey, TMessage> : IConsumeMiddleware<TKey, TMessage> { }

[MiddlewareOrder(1)]   // Executes last (closest to handler)
public class LoggingMiddleware<TKey, TMessage> : IConsumeMiddleware<TKey, TMessage> { }
```

---

## Custom Serialization

### Custom Serializer

```csharp
public class ProtobufSerializer<T> : ISerializer<T> where T : IMessage<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        return data.ToByteArray();
    }
}
```

### Custom Deserializer

```csharp
public class ProtobufDeserializer<T> : IDeserializer<T> where T : IMessage<T>, new()
{
    private readonly MessageParser<T> _parser = new(() => new T());

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.IsEmpty) return default!;
        return _parser.ParseFrom(data.ToArray());
    }
}
```

### Register Default Serializers

```csharp
builder.Services.AddKafkaBusProducers(
    builder.Configuration,
    [typeof(Program).Assembly],
    defaultValueSerializer: typeof(ProtobufSerializer<>));

builder.Services.AddKafkaBusConsumers(
    builder.Configuration,
    [typeof(Program).Assembly],
    defaultValueDeserializer: typeof(ProtobufDeserializer<>));
```

---

## Advanced Configuration

### Custom Configuration Section Names

```csharp
builder.Services.AddKafkaBusProducers(
    builder.Configuration,
    [typeof(Program).Assembly],
    sectionName: "Messaging:KafkaProducer");

builder.Services.AddKafkaBusConsumers(
    builder.Configuration,
    [typeof(Program).Assembly],
    sectionName: "Messaging:KafkaConsumer");
```

```json
{
  "Messaging": {
    "KafkaProducer": {
      "BootstrapServers": "kafka:9092",
      "Acks": "All"
    },
    "KafkaConsumer": {
      "BootstrapServers": "kafka:9092",
      "AutoOffsetReset": "Earliest",
      "EnableAutoCommit": true
    }
  }
}
```

### Multiple Assemblies

```csharp
builder.Services.AddKafkaBusProducers(builder.Configuration, [
    typeof(Program).Assembly,
    typeof(OrderCreated).Assembly,
    typeof(SharedMessages).Assembly
]);
```

---

## API Reference

### IMessageBus

| Method | Description |
|--------|-------------|
| `SendAsync<TMessage>(topic, message, headers?, ct)` | Send a message with string key |
| `SendAsync<TMessage>(topic, partition, message, headers?, ct)` | Send to specific partition |
| `SendBatchAsync<TMessage>(topic, messages, headers?, ct)` | Send multiple messages |
| `SendAsync<TKey, TMessage>(topic, message, headers?, ct)` | Send with custom key type |
| `SendAsync<TKey, TMessage>(topic, partition, message, headers?, ct)` | Send with custom key to partition |
| `SendBatchAsync<TKey, TMessage>(topic, messages, headers?, ct)` | Batch send with custom key |

### ConsumeContext<TKey, TMessage>

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `TKey` | Message key |
| `Message` | `TMessage` | Deserialized message |
| `Topic` | `string` | Source topic |
| `Partition` | `int` | Partition number |
| `Offset` | `long` | Message offset |
| `Headers` | `Headers?` | Message headers |
| `Timestamp` | `DateTime` | Message timestamp |

### MessageConsumer<TMessage>

| Method | Description |
|--------|-------------|
| `HandleAsync(context, ct)` | Process the message (abstract) |
| `Ack(context)` | Acknowledge single message |
| `Ack(contexts)` | Acknowledge multiple messages |

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.