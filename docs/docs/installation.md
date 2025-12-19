---
sidebar_position: 2
---

# Installation

## NuGet Package

Install KafkaBus via NuGet Package Manager:

```bash
dotnet add package KafkaBus
```

Or via Package Manager Console:

```powershell
Install-Package KafkaBus
```

## Package Reference

Add directly to your `.csproj` file:

```xml
<PackageReference Include="KafkaBus" Version="1.0.0" />
```

## Dependencies

KafkaBus automatically includes the following dependencies:

- [Confluent.Kafka](https://github.com/confluentinc/confluent-kafka-dotnet) - Apache Kafka client
- [Scrutor](https://github.com/khellang/Scrutor) - Assembly scanning for DI
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.Logging
- Microsoft.Extensions.Configuration

## Verify Installation

After installation, you should be able to use KafkaBus namespaces:

```csharp
using KafkaBus;
using KafkaBus.Abstractions;
```

## Next Steps

Continue to [Quick Start](quick-start) to configure and use KafkaBus in your application.
