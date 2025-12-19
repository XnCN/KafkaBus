---
sidebar_position: 100
---

# API Reference

Complete reference for KafkaBus interfaces and classes.

## Producer

### IMessageBus

Main interface for sending messages.

```csharp
public interface IMessageBus
{
    // String key methods
    Task<DeliveryResult<string, TMessage>> SendAsync<TMessage>(
        string topic,
        TMessage message,
        Headers? headers = null,
        CancellationToken ct = default);

    Task<DeliveryResult<string, TMessage>> SendAsync<TMessage>(
        string topic,
        int partition,
        TMessage message,
        Headers? headers = null,
        CancellationToken ct = default);

    Task<IEnumerable<DeliveryResult<string, TMessage>>> SendBatchAsync<TMessage>(
        string topic,
        IEnumerable<TMessage> messages,
        Headers? headers = null,
        CancellationToken ct = default);

    // Custom key methods
    Task<DeliveryResult<TKey, TMessage>> SendAsync<TKey, TMessage>(
        string topic,
        TMessage message,
        Headers? headers = null,
        CancellationToken ct = default);

    Task<DeliveryResult<TKey, TMessage>> SendAsync<TKey, TMessage>(
        string topic,
        int partition,
        TMessage message,
        Headers? headers = null,
        CancellationToken ct = default);

    Task<IEnumerable<DeliveryResult<TKey, TMessage>>> SendBatchAsync<TKey, TMessage>(
        string topic,
        IEnumerable<TMessage> messages,
        Headers? headers = null,
        CancellationToken ct = default);
}
```

### IProducerConfiguration\<TKey, TMessage\>

Configure producer behavior for specific message types.

```csharp
public interface IProducerConfiguration<TKey, TMessage>
{
    TKey GetKey(TMessage message);
    Task<ProducerConfig> ConfigureAsync();
    Task<ProducerBuilder<TKey, TMessage>> GenerateBuilderAsync();
    ISerializer<TKey>? KeySerializer { get; }
    ISerializer<TMessage>? ValueSerializer { get; }
}
```

### ProducerConfiguration\<TKey, TMessage\>

Base class for producer configurations.

```csharp
public class ProducerConfiguration<TKey, TMessage> : IProducerConfiguration<TKey, TMessage>
{
    protected readonly ProducerConfig defaultConfiguration;

    public virtual TKey GetKey(TMessage message) => default!;
    public virtual Task<ProducerConfig> ConfigureAsync();
    public virtual Task<ProducerBuilder<TKey, TMessage>> GenerateBuilderAsync();
    public virtual ISerializer<TKey>? KeySerializer { get; }
    public virtual ISerializer<TMessage>? ValueSerializer { get; }
}
```

### IProduceMiddleware\<TKey, TMessage\>

Middleware interface for producers.

```csharp
public interface IProduceMiddleware<TKey, TMessage>
{
    Task<DeliveryResult<TKey, TMessage>> InvokeAsync(
        ProduceContext<TKey, TMessage> context,
        ProduceDelegate<TKey, TMessage> next,
        CancellationToken ct);
}
```

### ProduceContext\<TKey, TMessage\>

Context passed through producer middleware.

```csharp
public sealed record ProduceContext<TKey, TMessage>(
    TKey Key,
    TMessage Message,
    string Topic,
    int? Partition,
    Headers? Headers);
```

### ProduceDelegate\<TKey, TMessage\>

Delegate for producer pipeline.

```csharp
public delegate Task<DeliveryResult<TKey, TMessage>> ProduceDelegate<TKey, TMessage>(
    ProduceContext<TKey, TMessage> context,
    CancellationToken ct);
```

---

## Consumer

### IMessageConsumer\<TKey, TMessage\>

Interface for message consumers.

```csharp
public interface IMessageConsumer<TKey, TMessage>
{
    Task HandleAsync(ConsumeContext<TKey, TMessage> context, CancellationToken ct = default);
}
```

### MessageConsumer\<TMessage\>

Base class for consumers with string key.

```csharp
public abstract class MessageConsumer<TMessage> : MessageConsumer<string, TMessage>
{
}
```

### MessageConsumer\<TKey, TMessage\>

Base class for consumers with custom key.

```csharp
public abstract class MessageConsumer<TKey, TMessage> : IMessageConsumer<TKey, TMessage>
{
    protected void Ack(ConsumeContext<TKey, TMessage> context);
    protected void Ack(IEnumerable<ConsumeContext<TKey, TMessage>> contexts);
    public abstract Task HandleAsync(ConsumeContext<TKey, TMessage> context, CancellationToken ct = default);
}
```

### ConsumeContext\<TKey, TMessage\>

Context containing message and metadata.

```csharp
public sealed record ConsumeContext<TKey, TMessage>(
    TKey Key,
    TMessage Message,
    string Topic,
    int Partition,
    long Offset,
    Headers? Headers,
    DateTime Timestamp,
    TopicPartitionOffset TopicPartitionOffset);
```

### IConsumerConfiguration\<TKey, TMessage\>

Configure consumer behavior.

```csharp
public interface IConsumerConfiguration<TKey, TMessage>
{
    int WorkerCount { get; }
    string Topic { get; }
    string GroupId { get; }
    Task<ConsumerConfig> ConfigureAsync();
    Task<ConsumerBuilder<TKey, TMessage>> GenerateBuilderAsync();
    IDeserializer<TKey>? KeyDeserializer { get; }
    IDeserializer<TMessage>? ValueDeserializer { get; }
}
```

### ConsumerConfiguration\<TKey, TMessage\>

Base class for consumer configurations.

```csharp
public class ConsumerConfiguration<TKey, TMessage> : IConsumerConfiguration<TKey, TMessage>
{
    protected readonly ConsumerConfig defaultConfiguration;

    public virtual int WorkerCount => 1;
    public virtual string Topic => typeof(TMessage).Name.ToLower();
    public virtual string GroupId => $"{Topic}-group";
    public virtual Task<ConsumerConfig> ConfigureAsync();
    public virtual Task<ConsumerBuilder<TKey, TMessage>> GenerateBuilderAsync();
    public virtual IDeserializer<TKey>? KeyDeserializer { get; }
    public virtual IDeserializer<TMessage>? ValueDeserializer { get; }
}
```

### IConsumeMiddleware\<TKey, TMessage\>

Middleware interface for consumers.

```csharp
public interface IConsumeMiddleware<TKey, TMessage>
{
    Task InvokeAsync(
        ConsumeContext<TKey, TMessage> context,
        ConsumeDelegate<TKey, TMessage> next,
        CancellationToken ct);
}
```

### ConsumeDelegate\<TKey, TMessage\>

Delegate for consumer pipeline.

```csharp
public delegate Task ConsumeDelegate<TKey, TMessage>(
    ConsumeContext<TKey, TMessage> context,
    CancellationToken ct);
```

---

## Attributes

### MiddlewareOrderAttribute

Control middleware execution order.

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class MiddlewareOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}
```

---

## Extensions

### ServiceCollectionExtensions

DI registration methods.

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaBusProducers(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly[] assemblies,
        string sectionName = KafkaConfigurationDefaults.ProducerSectionName,
        Type? defaultKeySerializer = null,
        Type? defaultValueSerializer = null);

    public static IServiceCollection AddKafkaBusConsumers(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly[] assemblies,
        string sectionName = KafkaConfigurationDefaults.ConsumerSectionName,
        Type? defaultKeyDeserializer = null,
        Type? defaultValueDeserializer = null);
}
```

---

## Constants

### KafkaConfigurationDefaults

Default configuration section names.

```csharp
public static class KafkaConfigurationDefaults
{
    public const string ProducerSectionName = "KafkaBus:DefaultProducerSettings";
    public const string ConsumerSectionName = "KafkaBus:DefaultConsumerSettings";
}
```
