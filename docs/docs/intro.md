---
sidebar_position: 1
slug: /
---

# Introduction

**KafkaBus** is a lightweight, middleware-enabled Kafka library for .NET that simplifies producer and consumer implementations with a clean, fluent API.

## Why KafkaBus?

Building Kafka applications with Confluent.Kafka can require significant boilerplate code. KafkaBus abstracts away the complexity while providing:

- **Simple API** - Send and receive messages with minimal code
- **Dependency Injection** - First-class support for Microsoft.Extensions.DependencyInjection
- **Middleware Pipeline** - Intercept and process messages with reusable middleware
- **Auto-Discovery** - Automatic registration of consumers and configurations
- **Flexibility** - Full control when you need it, sensible defaults when you don't

## Features

| Feature                 | Description                                            |
| ----------------------- | ------------------------------------------------------ |
| 🚀 Simple API           | Intuitive methods for producing and consuming messages |
| 🔌 DI Support           | Built-in dependency injection integration              |
| 🔗 Middleware           | Pipeline pattern for cross-cutting concerns            |
| ⚡ Batch Operations     | Send multiple messages efficiently                     |
| 🔄 Manual Ack           | Control message acknowledgment                         |
| 🎯 Custom Serialization | Use your own serializers                               |
| 📦 Auto-Discovery       | Convention-based configuration scanning                |

## Supported .NET Versions

KafkaBus supports the following .NET versions:

- .NET 6.0
- .NET 8.0
- .NET 9.0

## Next Steps

- [Installation](installation) - Add KafkaBus to your project
- [Quick Start](quick-start) - Get up and running in 5 minutes
- [Producer Guide](producer/basic-usage) - Learn how to send messages
- [Consumer Guide](consumer/basic-usage) - Learn how to receive messages
