using Confluent.Kafka;
using KafkaBus.Abstractions.Consumer;
using KafkaBus.Core.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaBus.Core.Consumer;

public class ConsumerConfiguration<TKey, TMessage>(IServiceProvider serviceProvider) : IConsumerConfiguration<TKey, TMessage>
{
    public virtual int WorkerCount => 1;
    public virtual string Topic => typeof(TMessage).Name.ToLower();
    public virtual string GroupId => $"{Topic}-group";

    public virtual IDeserializer<TKey>? KeySerializer => serviceProvider.GetService<IDeserializer<TKey>>();
    public virtual IDeserializer<TMessage>? ValueSerializer => serviceProvider.GetService<IDeserializer<TMessage>>() ?? new DefaultValueDeserializer<TMessage>();

    protected readonly ConsumerConfig defaultConfiguration = serviceProvider.GetRequiredService<ConsumerConfig>();

    public virtual Task<ConsumerConfig> ConfigureAsync()
    {
        defaultConfiguration.GroupId = GroupId;
        return Task.FromResult(defaultConfiguration);
    }

    public virtual async Task<ConsumerBuilder<TKey, TMessage>> GenerateBuilderAsync()
    {
        var configuration = await ConfigureAsync();
        var builder = new ConsumerBuilder<TKey, TMessage>(configuration);

        if (KeySerializer is not null)
            builder.SetKeyDeserializer(KeySerializer);

        if (ValueSerializer is not null)
            builder.SetValueDeserializer(ValueSerializer);

        return builder;
    }
}