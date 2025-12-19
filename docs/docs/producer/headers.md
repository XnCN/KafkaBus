---
sidebar_position: 4
---

# Headers

Headers allow you to attach metadata to messages without modifying the message payload.

## Adding Headers

Create a `Headers` object and pass it to send methods:

```csharp
var headers = new Headers
{
    { "correlation-id", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
    { "source", Encoding.UTF8.GetBytes("order-service") },
    { "version", Encoding.UTF8.GetBytes("1.0") }
};

await messageBus.SendAsync("orders", message, headers);
```

## With Custom Key Types

```csharp
var headers = new Headers
{
    { "trace-id", Encoding.UTF8.GetBytes(Activity.Current?.Id ?? "") }
};

await messageBus.SendAsync<Guid, OrderCreated>("orders", message, headers);
```

## With Batch Operations

```csharp
var headers = new Headers
{
    { "batch-id", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
    { "batch-size", Encoding.UTF8.GetBytes(messages.Count().ToString()) }
};

await messageBus.SendBatchAsync("orders", messages, headers);
```

## Common Header Patterns

### Correlation ID

Track requests across services:

```csharp
public async Task SendWithCorrelationAsync<T>(string topic, T message, string? correlationId = null)
{
    var headers = new Headers
    {
        { "correlation-id", Encoding.UTF8.GetBytes(correlationId ?? Guid.NewGuid().ToString()) }
    };

    await messageBus.SendAsync(topic, message, headers);
}
```

### Distributed Tracing

Propagate trace context:

```csharp
public async Task SendWithTracingAsync<T>(string topic, T message)
{
    var headers = new Headers();

    if (Activity.Current is not null)
    {
        headers.Add("trace-id", Encoding.UTF8.GetBytes(Activity.Current.TraceId.ToString()));
        headers.Add("span-id", Encoding.UTF8.GetBytes(Activity.Current.SpanId.ToString()));
    }

    await messageBus.SendAsync(topic, message, headers);
}
```

### Message Type

Include type information for polymorphic deserialization:

```csharp
public async Task SendTypedAsync<T>(string topic, T message)
{
    var headers = new Headers
    {
        { "message-type", Encoding.UTF8.GetBytes(typeof(T).AssemblyQualifiedName!) }
    };

    await messageBus.SendAsync(topic, message, headers);
}
```

### Timestamp and Source

Add audit information:

```csharp
public async Task SendWithAuditAsync<T>(string topic, T message, string source)
{
    var headers = new Headers
    {
        { "source", Encoding.UTF8.GetBytes(source) },
        { "produced-at", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
        { "machine", Encoding.UTF8.GetBytes(Environment.MachineName) }
    };

    await messageBus.SendAsync(topic, message, headers);
}
```

## Header Helper Extension

Create a helper for cleaner syntax:

```csharp
public static class HeadersExtensions
{
    public static Headers AddString(this Headers headers, string key, string value)
    {
        headers.Add(key, Encoding.UTF8.GetBytes(value));
        return headers;
    }

    public static Headers AddCorrelationId(this Headers headers, string? correlationId = null)
    {
        return headers.AddString("correlation-id", correlationId ?? Guid.NewGuid().ToString());
    }

    public static Headers AddTracing(this Headers headers)
    {
        if (Activity.Current is not null)
        {
            headers.AddString("trace-id", Activity.Current.TraceId.ToString());
            headers.AddString("span-id", Activity.Current.SpanId.ToString());
        }
        return headers;
    }
}

// Usage
var headers = new Headers()
    .AddCorrelationId()
    .AddTracing()
    .AddString("source", "order-service");

await messageBus.SendAsync("orders", message, headers);
```

## Next Steps

- [Configuration](configuration) - Advanced producer settings
- [Middleware](middleware) - Add headers automatically via middleware
