---
sidebar_position: 1
---

# Default Serialization

KafkaBus uses JSON serialization by default via `System.Text.Json`.

## Default Behavior

### Producer

Messages are serialized to JSON UTF-8 bytes:

```csharp
// This message
var order = new OrderCreated(Guid.NewGuid(), "Product", 99.99m);
await messageBus.SendAsync("orders", order);

// Is serialized as
// {"Id":"...","ProductName":"Product","Amount":99.99}
```

### Consumer

Messages are deserialized from JSON UTF-8 bytes:

```csharp
public override Task HandleAsync(
    ConsumeContext<string, OrderCreated> context,
    CancellationToken ct)
{
    // context.Message is already deserialized
    var order = context.Message;
    return Task.CompletedTask;
}
```

## Default Serializers

| Type         | Serializer                    | Deserializer                    |
| ------------ | ----------------------------- | ------------------------------- |
| Key (string) | StringSerializer (Confluent)  | StringDeserializer (Confluent)  |
| Value        | DefaultSerializer\<T\> (JSON) | DefaultDeserializer\<T\> (JSON) |

## JSON Options

The default serializer uses standard `System.Text.Json` options:

```csharp
public class DefaultSerializer<T> : ISerializer<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data);
    }
}

public class DefaultDeserializer<T> : IDeserializer<T>
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.IsEmpty)
            return default!;
        return JsonSerializer.Deserialize<T>(data)!;
    }
}
```

## Supported Types

The default JSON serializer supports:

- Records and classes
- Primitive types
- Collections (List, Array, Dictionary)
- Nested objects
- Nullable types

```csharp
// All these work with default serialization
public record OrderCreated(Guid Id, string ProductName, decimal Amount);

public class Order
{
    public Guid Id { get; set; }
    public List<OrderItem> Items { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

public record OrderItem(string ProductId, int Quantity, decimal? Discount);
```

## Next Steps

- [Custom Serialization](custom) - Use Protobuf, Avro, or other formats
