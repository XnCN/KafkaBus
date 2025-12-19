---
sidebar_position: 3
---

# Quick Start

Get KafkaBus running in your application in just 5 steps.

## Step 1: Configure Services

Register KafkaBus services in your `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add KafkaBus producers and consumers
builder.Services.AddKafkaBusProducers(builder.Configuration, [typeof(Program).Assembly]);
builder.Services.AddKafkaBusConsumers(builder.Configuration, [typeof(Program).Assembly]);

var app = builder.Build();
app.Run();
```

## Step 2: Add Configuration

Add Kafka settings to your `appsettings.json`:

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

## Step 3: Define Your Message

Create a message class or record:

```csharp
public record OrderCreated(Guid Id, string ProductName, decimal Amount);
```

## Step 4: Send Messages

Inject `IMessageBus` and send messages:

```csharp
public class OrderController(IMessageBus messageBus) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        var message = new OrderCreated(
            Guid.NewGuid(),
            request.ProductName,
            request.Amount
        );

        var result = await messageBus.SendAsync("orders", message);

        return Ok(new {
            MessageId = result.Offset.Value,
            Partition = result.Partition.Value
        });
    }
}
```

## Step 5: Consume Messages

Create a consumer class and its configuration:

```csharp
public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
    : MessageConsumer<OrderCreated>
{
    public override Task HandleAsync(
        ConsumeContext<string, OrderCreated> context,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Order received: {OrderId} - {ProductName}",
            context.Message.Id,
            context.Message.ProductName
        );

        return Task.CompletedTask;
    }
}

public class OrderCreatedConsumerConfiguration(IServiceProvider sp)
    : ConsumerConfiguration<string, OrderCreated>(sp)
{
    public override string Topic => "orders";
}
```

## That's It!

Your application is now ready to produce and consume Kafka messages. The consumer will automatically start listening when your application runs.

## What's Next?

Now that you have the basics working, explore more features:

- [Producer Features](producer/basic-usage) - Headers, batch operations, custom keys
- [Consumer Features](consumer/basic-usage) - Manual acknowledgment, multiple workers
- [Middleware](producer/middleware) - Add logging, retry logic, and more

## Complete Example

Here's a complete `Program.cs` for reference:

```csharp
using KafkaBus;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddKafkaBusProducers(builder.Configuration, [typeof(Program).Assembly]);
builder.Services.AddKafkaBusConsumers(builder.Configuration, [typeof(Program).Assembly]);

var app = builder.Build();

app.MapControllers();
app.Run();
```
