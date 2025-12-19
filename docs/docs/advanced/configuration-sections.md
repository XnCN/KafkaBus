---
sidebar_position: 1
---

# Configuration Sections

Customize the configuration section names used by KafkaBus.

## Default Section Names

By default, KafkaBus reads from:

- Producer: `KafkaBus:DefaultProducerSettings`
- Consumer: `KafkaBus:DefaultConsumerSettings`

```json
{
  "KafkaBus": {
    "DefaultProducerSettings": {
      "BootstrapServers": "localhost:9092"
    },
    "DefaultConsumerSettings": {
      "BootstrapServers": "localhost:9092"
    }
  }
}
```

## Custom Section Names

Specify custom section names when registering services:

```csharp
builder.Services.AddKafkaBusProducers(
    builder.Configuration,
    [typeof(Program).Assembly],
    sectionName: "Messaging:KafkaProducer"
);

builder.Services.AddKafkaBusConsumers(
    builder.Configuration,
    [typeof(Program).Assembly],
    sectionName: "Messaging:KafkaConsumer"
);
```

With matching configuration:

```json
{
  "Messaging": {
    "KafkaProducer": {
      "BootstrapServers": "kafka:9092",
      "Acks": "All"
    },
    "KafkaConsumer": {
      "BootstrapServers": "kafka:9092",
      "AutoOffsetReset": "Earliest"
    }
  }
}
```

## Multiple Kafka Clusters

Use different sections for different clusters:

```csharp
// Main cluster
builder.Services.AddKafkaBusProducers(
    builder.Configuration,
    [typeof(Program).Assembly],
    sectionName: "Kafka:MainCluster:Producer"
);

// Analytics cluster
builder.Services.AddKafkaBusProducers(
    builder.Configuration,
    [typeof(AnalyticsMessages).Assembly],
    sectionName: "Kafka:AnalyticsCluster:Producer"
);
```

```json
{
  "Kafka": {
    "MainCluster": {
      "Producer": {
        "BootstrapServers": "main-kafka:9092"
      }
    },
    "AnalyticsCluster": {
      "Producer": {
        "BootstrapServers": "analytics-kafka:9092"
      }
    }
  }
}
```

## Environment-Specific Configuration

Combine with environment-specific files:

```json
// appsettings.json (base)
{
  "KafkaBus": {
    "DefaultProducerSettings": {
      "Acks": "All",
      "EnableIdempotence": true
    }
  }
}

// appsettings.Development.json
{
  "KafkaBus": {
    "DefaultProducerSettings": {
      "BootstrapServers": "localhost:9092"
    }
  }
}

// appsettings.Production.json
{
  "KafkaBus": {
    "DefaultProducerSettings": {
      "BootstrapServers": "kafka-1:9092,kafka-2:9092,kafka-3:9092",
      "SecurityProtocol": "SaslSsl",
      "SaslMechanism": "Plain",
      "SaslUsername": "${KAFKA_USERNAME}",
      "SaslPassword": "${KAFKA_PASSWORD}"
    }
  }
}
```

## Configuration Constants

KafkaBus provides default constants:

```csharp
public static class KafkaConfigurationDefaults
{
    public const string ProducerSectionName = "KafkaBus:DefaultProducerSettings";
    public const string ConsumerSectionName = "KafkaBus:DefaultConsumerSettings";
}
```

Use these for consistency:

```csharp
var producerConfig = configuration
    .GetSection(KafkaConfigurationDefaults.ProducerSectionName)
    .Get<ProducerConfig>();
```

## Next Steps

- [Multiple Assemblies](multiple-assemblies) - Scan multiple assemblies
