---
sidebar_position: 2
---

# Custom Serialization

Implement custom serializers for Protobuf, Avro, MessagePack, or other formats.

## Marker Interfaces

KafkaBus provides marker interfaces to identify default serializers:

```csharp
// Key serializers
public interface IDefaultKeySerializer { }
public interface IDefaultKeyDeserializer { }

// Value serializers
public interface IDefaultValueSerializer { }
public interface IDefaultValueDeserializer { }
```

## Custom Serializer Interface

Implement `ISerializer<T>` from Confluent.Kafka and add the marker interface:

```csharp
public class CustomSerializer<T> : ISerializer<T>, IDefaultValueSerializer
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        // Your serialization logic
    }
}
```

## Custom Deserializer Interface

Implement `IDeserializer<T>` with the marker interface:

```csharp
public class CustomDeserializer<T> : IDeserializer<T>, IDefaultValueDeserializer
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.IsEmpty)
            return default!;
        // Your deserialization logic
    }
}
```

## Protobuf Example

Using Google.Protobuf with marker interfaces:

```csharp
public class ProtobufSerializer<T> : ISerializer<T>, IDefaultValueSerializer
    where T : IMessage<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        return data.ToByteArray();
    }
}

public class ProtobufDeserializer<T> : IDeserializer<T>, IDefaultValueDeserializer
    where T : IMessage<T>, new()
{
    private readonly MessageParser<T> _parser = new(() => new T());

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.IsEmpty)
            return default!;
        return _parser.ParseFrom(data.ToArray());
    }
}
```

## MessagePack Example

Using MessagePack-CSharp with marker interfaces:

```csharp
public class MessagePackSerializer<T> : ISerializer<T>, IDefaultValueSerializer
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        return MessagePackSerializer.Serialize(data);
    }
}

public class MessagePackDeserializer<T> : IDeserializer<T>, IDefaultValueDeserializer
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.IsEmpty)
            return default!;
        return MessagePackSerializer.Deserialize<T>(data.ToArray());
    }
}
```

## Registering Default Serializers

Register globally for all producers/consumers:

```csharp
// Use Protobuf for all messages
builder.Services.AddKafkaBusProducers(
    builder.Configuration,
    [typeof(Program).Assembly],
    defaultValueSerializer: typeof(ProtobufSerializer<>)
);

builder.Services.AddKafkaBusConsumers(
    builder.Configuration,
    [typeof(Program).Assembly],
    defaultValueDeserializer: typeof(ProtobufDeserializer<>)
);
```

## Per-Message Serializers

Configure for specific message types:

```csharp
public class OrderProducerConfiguration(IServiceProvider sp)
    : ProducerConfiguration<string, OrderCreated>(sp)
{
    public override ISerializer<OrderCreated>? ValueSerializer
        => new ProtobufSerializer<OrderCreated>();
}

public class OrderConsumerConfiguration(IServiceProvider sp)
    : ConsumerConfiguration<string, OrderCreated>(sp)
{
    public override string Topic => "orders";

    public override IDeserializer<OrderCreated>? ValueDeserializer
        => new ProtobufDeserializer<OrderCreated>();
}
```

## Custom Key Serializers

For non-string keys, use `IDefaultKeySerializer` and `IDefaultKeyDeserializer`:

```csharp
public class GuidSerializer : ISerializer<Guid>, IDefaultKeySerializer
{
    public byte[] Serialize(Guid data, SerializationContext context)
    {
        return data.ToByteArray();
    }
}

public class GuidDeserializer : IDeserializer<Guid>, IDefaultKeyDeserializer
{
    public Guid Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.Length != 16)
            return Guid.Empty;
        return new Guid(data);
    }
}

// Usage in configuration
public class OrderProducerConfiguration(IServiceProvider sp)
    : ProducerConfiguration<Guid, OrderCreated>(sp)
{
    public override Guid GetKey(OrderCreated message) => message.Id;
    public override ISerializer<Guid>? KeySerializer => new GuidSerializer();
}
```

## Schema Registry Integration

For Avro with Confluent Schema Registry:

```csharp
public class AvroSerializerWrapper<T> : ISerializer<T>, IDefaultValueSerializer
    where T : class
{
    private readonly ISchemaRegistryClient _schemaRegistry;
    private readonly AvroSerializer<T> _serializer;

    public AvroSerializerWrapper(string schemaRegistryUrl)
    {
        _schemaRegistry = new CachedSchemaRegistryClient(
            new SchemaRegistryConfig { Url = schemaRegistryUrl });
        _serializer = new AvroSerializer<T>(_schemaRegistry);
    }

    public byte[] Serialize(T data, SerializationContext context)
    {
        return _serializer.SerializeAsync(data, context)
            .GetAwaiter().GetResult();
    }
}
```

## Marker Interface Summary

| Interface                   | Purpose                                         |
| --------------------------- | ----------------------------------------------- |
| `IDefaultKeySerializer`     | Marks a class as the default key serializer     |
| `IDefaultKeyDeserializer`   | Marks a class as the default key deserializer   |
| `IDefaultValueSerializer`   | Marks a class as the default value serializer   |
| `IDefaultValueDeserializer` | Marks a class as the default value deserializer |

## Next Steps

- [Advanced Configuration](../advanced/configuration-sections) - Custom config sections
- [Multiple Assemblies](../advanced/multiple-assemblies) - Scan multiple assemblies
