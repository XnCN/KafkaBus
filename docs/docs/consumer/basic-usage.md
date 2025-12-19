---
sidebar_position: 1
---

# Basic Usage

Learn how to consume messages from Kafka using KafkaBus.

## Creating a Consumer

Extend `MessageConsumer<TMessage>` for string keys or `MessageConsumer<TKey, TMessage>` for custom keys:

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
```

## Consumer Configuration

Every consumer needs a configuration class:

```csharp
public class OrderCreatedConsumerConfiguration(IServiceProvider sp)
    : ConsumerConfiguration<string, OrderCreated>(sp)
{
    public override string Topic => "orders";
}
```

## Custom Key Types

For non-string keys:

```csharp
public class PaymentConsumer : MessageConsumer<Guid, PaymentProcessed>
{
    public override Task HandleAsync(
        ConsumeContext<Guid, PaymentProcessed> context,
        CancellationToken ct)
    {
        Console.WriteLine($"Payment Key: {context.Key}");
        Console.WriteLine($"Amount: {context.Message.Amount}");
        return Task.CompletedTask;
    }
}

public class PaymentConsumerConfiguration(IServiceProvider sp)
    : ConsumerConfiguration<Guid, PaymentProcessed>(sp)
{
    public override string Topic => "payments";

    public override IDeserializer<Guid>? KeyDeserializer
        => new GuidDeserializer();
}
```

## Async Processing

Perform async operations in your handler:

```csharp
public class OrderConsumer(
    IOrderRepository repository,
    IEmailService emailService,
    ILogger<OrderConsumer> logger)
    : MessageConsumer<OrderCreated>
{
    public override async Task HandleAsync(
        ConsumeContext<string, OrderCreated> context,
        CancellationToken ct)
    {
        var order = context.Message;

        // Save to database
        await repository.SaveAsync(new Order
        {
            Id = order.Id,
            ProductName = order.ProductName,
            Amount = order.Amount,
            CreatedAt = context.Timestamp
        }, ct);

        // Send confirmation email
        await emailService.SendOrderConfirmationAsync(order, ct);

        logger.LogInformation("Order {OrderId} processed", order.Id);
    }
}
```

## Error Handling

Exceptions in handlers are logged and the message processing continues:

```csharp
public override async Task HandleAsync(
    ConsumeContext<string, OrderCreated> context,
    CancellationToken ct)
{
    try
    {
        await ProcessOrderAsync(context.Message, ct);
    }
    catch (ValidationException ex)
    {
        // Log and skip invalid messages
        logger.LogWarning(ex, "Invalid order: {OrderId}", context.Message.Id);
    }
    catch (Exception ex)
    {
        // Re-throw to trigger retry (if auto-commit is off)
        logger.LogError(ex, "Failed to process order: {OrderId}", context.Message.Id);
        throw;
    }
}
```

## Dependency Injection

Consumers support full dependency injection:

```csharp
public class OrderConsumer(
    IOrderService orderService,
    IInventoryService inventoryService,
    IPaymentService paymentService,
    ILogger<OrderConsumer> logger)
    : MessageConsumer<OrderCreated>
{
    public override async Task HandleAsync(
        ConsumeContext<string, OrderCreated> context,
        CancellationToken ct)
    {
        var order = context.Message;

        // Use injected services
        await inventoryService.ReserveAsync(order.ProductName, ct);
        await paymentService.ProcessAsync(order.Amount, ct);
        await orderService.ConfirmAsync(order.Id, ct);
    }
}
```

## Auto-Start

Consumers automatically start when your application runs. No additional code needed:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKafkaBusConsumers(
    builder.Configuration,
    [typeof(Program).Assembly]
);

var app = builder.Build();
app.Run();  // Consumers start automatically
```

## Next Steps

- [Configuration](configuration) - Customize consumer behavior
- [Acknowledgment](acknowledgment) - Manual message acknowledgment
- [Metadata](metadata) - Access message metadata
