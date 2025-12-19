using Confluent.Kafka;

namespace KafkaBus.Abstractions.Producer;

public interface IMessageBus
{
    Task<DeliveryResult<string, TMessage>> SendAsync<TMessage>(string topic, TMessage message, Headers? headers = null, CancellationToken cancellationToken = default);
    Task<DeliveryResult<string, TMessage>> SendAsync<TMessage>(string topic, int partition, TMessage message, Headers? headers = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeliveryResult<string, TMessage>>> SendBatchAsync<TMessage>(string topic, IEnumerable<TMessage> messages, Headers? headers = null, CancellationToken cancellationToken = default);


    Task<DeliveryResult<TKey, TMessage>> SendAsync<TKey, TMessage>(string topic, TMessage message, Headers? headers = null, CancellationToken cancellationToken = default);
    Task<DeliveryResult<TKey, TMessage>> SendAsync<TKey, TMessage>(string topic, int partition, TMessage message, Headers? headers = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<DeliveryResult<TKey, TMessage>>> SendBatchAsync<TKey, TMessage>(string topic, IEnumerable<TMessage> messages, Headers? headers = null, CancellationToken cancellationToken = default);
}