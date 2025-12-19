using Confluent.Kafka;
using KafkaBus.Abstractions.Consumer;
using KafkaBus.Abstractions.Producer;
using KafkaBus.Abstractions.Serialization;
using KafkaBus.Core.Consumer;
using KafkaBus.Core.Producer;
using KafkaBus.Domain.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace KafkaBus.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaBusProducers(this IServiceCollection services, IConfiguration configuration, Assembly[] assemblies, Type? defaultKeySerializer = null, Type? defaultValueSerializer = null)
    {
        services.AddSingleton(configuration.GetSection("Kafka:DefaultProducerSettings").Get<ProducerConfig>() ?? new ProducerConfig());

        if (defaultKeySerializer is not null)
            services.AddSingleton(typeof(IDefaultKeyDeserializer), defaultKeySerializer);

        if (defaultValueSerializer is not null)
            services.AddSingleton(typeof(IDefaultValueDeserializer), defaultValueSerializer);

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(IProducerConfiguration<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.Scan(scan => scan
        .FromAssemblies(assemblies)
        .AddClasses(c => c.AssignableTo(typeof(IProduceMiddleware<,>)))
        .AsImplementedInterfaces()
        .WithSingletonLifetime());

        services.AddSingleton<IMessageBus, MessageBus>();

        return services;
    }


    public static IServiceCollection AddKafkaConsumers(this IServiceCollection services, IConfiguration configuration, Assembly[] assemblies, Type? defaultKeyDeserializer = null, Type? defaultValueDeserializer = null)
    {

        services.AddSingleton(configuration
            .GetSection("Kafka:DefaultConsumerSettings")
            .Get<ConsumerConfig>() ?? new ConsumerConfig());

        if (defaultKeyDeserializer is not null)
            services.AddSingleton(typeof(IDefaultKeyDeserializer), defaultKeyDeserializer);

        if (defaultValueDeserializer is not null)
            services.AddSingleton(typeof(IDefaultValueDeserializer), defaultValueDeserializer);

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(IMessageConsumer<,>)))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo(typeof(IConsumerConfiguration<,>)))
            .AsImplementedInterfaces()
            .WithSingletonLifetime());


        services.Scan(scan => scan
          .FromAssemblies(assemblies)
          .AddClasses(c => c.AssignableTo(typeof(IConsumeMiddleware<,>)))
          .AsImplementedInterfaces()
          .WithSingletonLifetime());

        services.TryAddSingleton(typeof(IConsumerConfiguration<,>), typeof(ConsumerConfiguration<,>));

        RegisterHostedServices(services, assemblies);
        return services;
    }

    private static void RegisterHostedServices(IServiceCollection services, Assembly[] assemblies)
    {
        var consumerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageConsumer<,>))
                .Select(i => i.GetGenericArguments()))
            .Distinct();

        foreach (var args in consumerTypes)
        {
            var hostedServiceType = typeof(ConsumerHostedService<,>).MakeGenericType(args);
            services.AddSingleton(typeof(IHostedService), hostedServiceType);
        }
    }
}
