using Confluent.Kafka;
using KafkaBus.Abstractions.Producer;
using KafkaBus.Domain.Attributes;
using KafkaBus.Domain.Producer;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace KafkaBus.Core.Producer;

public class MessageBus(IServiceScopeFactory serviceScopeFactory) : IMessageBus, IDisposable, IAsyncDisposable
{
    private readonly ConcurrentDictionary<Type, object> _contexts = new();
    private readonly ConcurrentDictionary<Type, object> _pipelines = new();


    public async Task<DeliveryResult<string, TMessage>> SendAsync<TMessage>(string topic, TMessage message, Headers? headers = null, CancellationToken cancellationToken = default)
        => await SendAsync<string, TMessage>(topic, message, headers, cancellationToken);

    public async Task<DeliveryResult<string, TMessage>> SendAsync<TMessage>(string topic, int partition, TMessage message, Headers? headers = null, CancellationToken cancellationToken = default)
        => await SendAsync<string, TMessage>(topic, partition, message, headers, cancellationToken);

    public async Task<IEnumerable<DeliveryResult<string, TMessage>>> SendBatchAsync<TMessage>(string topic, IEnumerable<TMessage> messages, Headers? headers = null, CancellationToken cancellationToken = default)
        => await SendBatchAsync<string, TMessage>(topic, messages, headers, cancellationToken);

    public async Task<DeliveryResult<TKey, TMessage>> SendAsync<TKey, TMessage>(string topic, TMessage message, Headers? headers = null, CancellationToken cancellationToken = default)
    {
        var context = await GetOrCreateContextAsync<TKey, TMessage>();
        var key = context.Configuration.GetKey(message);
        var pipeline = BuildPipeline(context);
        var produceContext = new ProduceContext<TKey, TMessage>(key, message, topic, null, headers);

        return await pipeline(produceContext, cancellationToken);
    }

    public async Task<DeliveryResult<TKey, TMessage>> SendAsync<TKey, TMessage>(string topic, int partition, TMessage message, Headers? headers = null, CancellationToken cancellationToken = default)
    {
        var context = await GetOrCreateContextAsync<TKey, TMessage>();
        var key = context.Configuration.GetKey(message);
        var pipeline = BuildPipeline(context);
        var produceContext = new ProduceContext<TKey, TMessage>(key, message, topic, partition, headers);

        return await pipeline(produceContext, cancellationToken);
    }

    public async Task<IEnumerable<DeliveryResult<TKey, TMessage>>> SendBatchAsync<TKey, TMessage>(string topic, IEnumerable<TMessage> messages, Headers? headers = null, CancellationToken cancellationToken = default)
    {
        var context = await GetOrCreateContextAsync<TKey, TMessage>();
        var tasks = new List<Task<DeliveryResult<TKey, TMessage>>>();

        foreach (var message in messages)
        {
            var key = context.Configuration.GetKey(message);
            var task = context.Producer.ProduceAsync(topic, new Message<TKey, TMessage>
            {
                Key = key!,
                Headers = headers,
                Value = message,
            }, cancellationToken);
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);
        return results;
    }

    private async Task<ProducerContext<TKey, TMessage>> GetOrCreateContextAsync<TKey, TMessage>()
    {
        var key = typeof(TMessage);

        if (_contexts.TryGetValue(key, out var existing))
            return (ProducerContext<TKey, TMessage>)existing;

        using var scope = serviceScopeFactory.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IProducerConfiguration<TKey, TMessage>>();
        var builder = await configuration.GenerateBuilderAsync();
        var producer = builder.Build();

        var context = new ProducerContext<TKey, TMessage>(producer, configuration);
        return (ProducerContext<TKey, TMessage>)_contexts.GetOrAdd(key, context);
    }

    private ProduceDelegate<TKey, TMessage> BuildPipeline<TKey, TMessage>(ProducerContext<TKey, TMessage> context)
    {
        var key = typeof((TKey, TMessage));
        if (_pipelines.TryGetValue(key, out var cached))
            return (ProduceDelegate<TKey, TMessage>)cached;

        using var scope = serviceScopeFactory.CreateScope();

        ProduceDelegate<TKey, TMessage> pipeline = async (ctx, ct) =>
        {
            var message = new Message<TKey, TMessage> { Key = ctx.Key, Headers = ctx.Headers, Value = ctx.Message };
            return ctx.Partition.HasValue
                ? await context.Producer.ProduceAsync(new TopicPartition(ctx.Topic, new Partition(ctx.Partition.Value)), message, ct)
                : await context.Producer.ProduceAsync(ctx.Topic, message, ct);
        };

        var middlewares = scope.ServiceProvider
            .GetServices<IProduceMiddleware<TKey, TMessage>>()
            .OrderByDescending(m => m.GetType().GetCustomAttribute<MiddlewareOrderAttribute>()?.Order ?? 0)
            .ToList();

        foreach (var middleware in middlewares)
        {
            var next = pipeline;
            pipeline = (ctx, ct) => middleware.InvokeAsync(ctx, next, ct);
        }

        _pipelines.TryAdd(key, pipeline);
        return pipeline;
    }

    public void Dispose()
    {
        foreach (var context in _contexts.Values)
            ((IDisposable)context.GetType().GetProperty("Producer")!.GetValue(context)!)?.Dispose();
        _contexts.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.CompletedTask;
    }
}