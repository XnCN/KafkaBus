using Confluent.Kafka;
using KafkaBus.Core.Serialization;
using KafkaBus.Domain.Producer;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaBus.Core.Producer;

public class ProducerConfiguration<TKey, TMessage>(IServiceProvider serviceProvider) : IProducerConfiguration<TKey, TMessage>
{
    public virtual TKey GetKey(TMessage message) => default!;
    public virtual ISerializer<TKey>? KeySerializer => serviceProvider.GetService<ISerializer<TKey>>();
    public virtual ISerializer<TMessage>? ValueSerializer => serviceProvider.GetService<ISerializer<TMessage>>() ?? new DefaultSerializer<TMessage>();

    protected readonly ProducerConfig defaultConfiguration = serviceProvider.GetRequiredService<ProducerConfig>();
    public virtual Task<ProducerConfig> ConfigureAsync() => Task.FromResult(defaultConfiguration);

    public virtual async Task<ProducerBuilder<TKey, TMessage>> GenerateBuilderAsync()
    {
        var configuration = await ConfigureAsync();
        var builder = new ProducerBuilder<TKey, TMessage>(configuration);

        if (KeySerializer is not null)
            builder.SetKeySerializer(KeySerializer);

        if (ValueSerializer is not null)
            builder.SetValueSerializer(ValueSerializer);

        return builder;
    }
}