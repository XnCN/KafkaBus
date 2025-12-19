using KafkaBus.Abstractions.Consumer;
using KafkaBus.Domain.Attributes;
using KafkaBus.Domain.Consumer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace KafkaBus.Core.Consumer;

public class ConsumerHostedService<TKey, TMessage>(IConsumerConfiguration<TKey, TMessage> configuration, IMessageConsumer<TKey, TMessage> consumer, IServiceProvider serviceProvider, ILogger<ConsumerHostedService<TKey, TMessage>> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        logger.LogInformation("Starting consumer for {Topic}", configuration.Topic);
        var pipeline = BuildPipeline();
        var tasks = Enumerable.Range(0, configuration.WorkerCount).Select(workerId =>
                RunWorkerAsync(workerId, pipeline, stoppingToken)
        );
        await Task.WhenAll(tasks);
    }


    private ConsumeDelegate<TKey, TMessage> BuildPipeline()
    {
        ConsumeDelegate<TKey, TMessage> pipeline = consumer.HandleAsync;

        var middlewares = serviceProvider
            .GetServices<IConsumeMiddleware<TKey, TMessage>>()
            .OrderByDescending(m => m.GetType().GetCustomAttribute<MiddlewareOrderAttribute>()?.Order ?? 0)
            .ToList();

        foreach (var middleware in middlewares)
        {
            var next = pipeline;
            pipeline = (ctx, ct) => middleware.InvokeAsync(ctx, next, ct);
        }

        return pipeline;
    }

    private async Task RunWorkerAsync(int workerId, ConsumeDelegate<TKey, TMessage> pipeline, CancellationToken stoppingToken)
    {
        await Task.Yield();
        var builder = await configuration.GenerateBuilderAsync();
        var kafkaConsumer = builder.Build();
        kafkaConsumer.Subscribe(configuration.Topic);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = kafkaConsumer.Consume(stoppingToken);
                if (result?.Message is null) continue;
                var context = new ConsumeContext<TKey, TMessage>(result.Message.Key, result.Message.Value, result.Topic, result.Partition.Value, result.Offset.Value, result.Message.Headers, result.Message.Timestamp.UtcDateTime);
                try
                {
                    await pipeline(context, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error handling message from {Topic}", configuration.Topic);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            kafkaConsumer.Unsubscribe();
            kafkaConsumer.Dispose();
        }
    }
}