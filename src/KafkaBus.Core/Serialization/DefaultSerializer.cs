using Confluent.Kafka;
using KafkaBus.Abstractions.Serialization;
using System.Text.Json;

namespace KafkaBus.Core.Serialization;

public class DefaultSerializer<T> : ISerializer<T>, IDefaultValueSerializer
{
    public byte[] Serialize(T data, SerializationContext context) => JsonSerializer.SerializeToUtf8Bytes(data);
}