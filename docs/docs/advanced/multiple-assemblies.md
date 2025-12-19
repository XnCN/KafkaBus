---
sidebar_position: 2
---

# Multiple Assemblies

Scan multiple assemblies for consumers, configurations, and middleware.

## Basic Usage

Pass an array of assemblies:

```csharp
builder.Services.AddKafkaBusProducers(builder.Configuration, [
    typeof(Program).Assembly,
    typeof(OrderCreated).Assembly,
    typeof(SharedMessages).Assembly
]);

builder.Services.AddKafkaBusConsumers(builder.Configuration, [
    typeof(Program).Assembly,
    typeof(OrderConsumer).Assembly,
    typeof(SharedConsumers).Assembly
]);
```

## Project Structure Example

```
Solution/
├── Api/
│   ├── Program.cs
│   └── Controllers/
├── Domain/
│   └── Messages/
│       ├── OrderCreated.cs
│       └── PaymentProcessed.cs
├── Application/
│   └── Consumers/
│       ├── OrderConsumer.cs
│       └── PaymentConsumer.cs
├── Infrastructure/
│   └── Kafka/
│       ├── Configurations/
│       └── Middleware/
└── Shared/
    └── Messages/
        └── CommonMessages.cs
```

Registration in `Program.cs`:

```csharp
var assemblies = new[]
{
    typeof(Program).Assembly,           // Api
    typeof(OrderCreated).Assembly,      // Domain
    typeof(OrderConsumer).Assembly,     // Application
    typeof(LoggingMiddleware<,>).Assembly  // Infrastructure
};

builder.Services.AddKafkaBusProducers(builder.Configuration, assemblies);
builder.Services.AddKafkaBusConsumers(builder.Configuration, assemblies);
```

## Assembly Discovery Helper

Create a helper for cleaner registration:

```csharp
public static class AssemblyHelper
{
    public static Assembly[] GetKafkaAssemblies() =>
    [
        typeof(Program).Assembly,
        typeof(OrderCreated).Assembly,
        typeof(OrderConsumer).Assembly,
        typeof(LoggingMiddleware<,>).Assembly
    ];
}

// Usage
builder.Services.AddKafkaBusProducers(
    builder.Configuration,
    AssemblyHelper.GetKafkaAssemblies()
);
```

## What Gets Scanned

KafkaBus scans assemblies for:

| Type                    | Interface                                |
| ----------------------- | ---------------------------------------- |
| Producer Configurations | `IProducerConfiguration<TKey, TMessage>` |
| Consumer Configurations | `IConsumerConfiguration<TKey, TMessage>` |
| Consumers               | `IMessageConsumer<TKey, TMessage>`       |
| Producer Middleware     | `IProduceMiddleware<TKey, TMessage>`     |
| Consumer Middleware     | `IConsumeMiddleware<TKey, TMessage>`     |

## Selective Scanning

For large solutions, be selective to improve startup time:

```csharp
// Only scan specific assemblies
var kafkaAssemblies = AppDomain.CurrentDomain
    .GetAssemblies()
    .Where(a => a.FullName?.Contains("Kafka") == true
             || a.FullName?.Contains("Messaging") == true)
    .ToArray();

builder.Services.AddKafkaBusConsumers(builder.Configuration, kafkaAssemblies);
```

## Modular Registration

For microservices with shared libraries:

```csharp
// In shared library
public static class KafkaModule
{
    public static IServiceCollection AddOrderKafka(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var assemblies = new[] { typeof(KafkaModule).Assembly };

        services.AddKafkaBusProducers(configuration, assemblies);
        services.AddKafkaBusConsumers(configuration, assemblies);

        return services;
    }
}

// In API project
builder.Services.AddOrderKafka(builder.Configuration);
```

## Next Steps

- [API Reference](../api-reference) - Complete API documentation
