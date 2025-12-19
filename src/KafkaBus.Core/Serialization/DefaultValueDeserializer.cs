using Confluent.Kafka;
using KafkaBus.Abstractions.Serialization;
using System.Text.Json;

namespace KafkaBus.Core.Serialization;

public class DefaultValueDeserializer<T> : IDeserializer<T>, IDefaultValueDeserializer
{
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.IsEmpty)
            return default!;
        return JsonSerializer.Deserialize<T>(data)!;
    }
}