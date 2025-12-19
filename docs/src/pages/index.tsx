import Link from "@docusaurus/Link";
import useDocusaurusContext from "@docusaurus/useDocusaurusContext";
import Layout from "@theme/Layout";
import Heading from "@theme/Heading";
import CodeBlock from "@theme/CodeBlock";

function HeroSection() {
  const { siteConfig } = useDocusaurusContext();
  return (
    <header className="hero hero--primary heroBanner">
      <div className="container">
        <Heading as="h1" className="hero__title">
          {siteConfig.title}
        </Heading>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div className="buttons">
          <Link className="button button--secondary button--lg" to="/docs">
            Get Started →
          </Link>
          <Link
            className="button button--outline button--lg margin-left--md"
            to="https://github.com/XnCN/KafkaBus"
          >
            GitHub
          </Link>
        </div>
      </div>
    </header>
  );
}

function InstallSection() {
  return (
    <section className="section">
      <div className="container">
        <div className="row">
          <div className="col col--8 col--offset-2">
            <Heading as="h2" className="text--center margin-bottom--lg">
              Quick Installation
            </Heading>
            <CodeBlock language="bash">dotnet add package KafkaBus</CodeBlock>
          </div>
        </div>
      </div>
    </section>
  );
}

const features = [
  {
    title: "Simple API",
    icon: "🚀",
    description:
      "Send and receive Kafka messages with just a few lines of code. No boilerplate required.",
  },
  {
    title: "Middleware Pipeline",
    icon: "🔗",
    description:
      "Add logging, retry logic, metrics, and more with the middleware pipeline pattern.",
  },
  {
    title: "DI Integration",
    icon: "🔌",
    description:
      "First-class support for Microsoft.Extensions.DependencyInjection with auto-discovery.",
  },
  {
    title: "Manual Acknowledgment",
    icon: "🔄",
    description:
      "Full control over message acknowledgment for reliable processing.",
  },
  {
    title: "Custom Serialization",
    icon: "🎯",
    description:
      "Use JSON, Protobuf, MessagePack, or any custom serializer you need.",
  },
  {
    title: "Batch Operations",
    icon: "⚡",
    description: "Send multiple messages efficiently with batch operations.",
  },
];

function FeaturesSection() {
  return (
    <section className="section sectionAlt">
      <div className="container">
        <Heading as="h2" className="text--center margin-bottom--lg">
          Features
        </Heading>
        <div className="row">
          {features.map((feature, idx) => (
            <div key={idx} className="col col--4 margin-bottom--lg">
              <div className="card padding--lg">
                <div className="text--center">
                  <span style={{ fontSize: "3rem" }}>{feature.icon}</span>
                </div>
                <Heading as="h3" className="text--center">
                  {feature.title}
                </Heading>
                <p className="text--center">{feature.description}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

function ProducerExample() {
  const code = `// Send a message
await messageBus.SendAsync("orders", new OrderCreated
{
    Id = Guid.NewGuid(),
    ProductName = "Laptop",
    Amount = 999.99m
});

// Send with custom key
await messageBus.SendAsync<Guid, OrderCreated>("orders", message);

// Send batch
await messageBus.SendBatchAsync("orders", messages);`;

  return (
    <div>
      <Heading as="h3">Producer</Heading>
      <CodeBlock language="csharp">{code}</CodeBlock>
    </div>
  );
}

function ConsumerExample() {
  const code = `public class OrderConsumer(ILogger<OrderConsumer> logger) 
    : MessageConsumer<OrderCreated>
{
    public override async Task HandleAsync(
        ConsumeContext<string, OrderCreated> context, 
        CancellationToken ct)
    {
        logger.LogInformation("Order received: {Id}", context.Message.Id);
        
        await ProcessOrderAsync(context.Message, ct);
        
        Ack(context); // Manual acknowledgment
    }
}

public class OrderConsumerConfiguration(IServiceProvider sp) 
    : ConsumerConfiguration<string, OrderCreated>(sp)
{
    public override string Topic => "orders";
}`;

  return (
    <div>
      <Heading as="h3">Consumer</Heading>
      <CodeBlock language="csharp">{code}</CodeBlock>
    </div>
  );
}

function CodeExamplesSection() {
  return (
    <section className="section">
      <div className="container">
        <Heading as="h2" className="text--center margin-bottom--lg">
          Simple & Intuitive
        </Heading>
        <div className="row">
          <div className="col col--6">
            <ProducerExample />
          </div>
          <div className="col col--6">
            <ConsumerExample />
          </div>
        </div>
      </div>
    </section>
  );
}

function ConfigExample() {
  const code = `{
  "KafkaBus": {
    "DefaultProducerSettings": {
      "BootstrapServers": "localhost:9092"
    },
    "DefaultConsumerSettings": {
      "BootstrapServers": "localhost:9092",
      "AutoOffsetReset": "Earliest",
      "EnableAutoCommit": true
    }
  }
}`;

  return (
    <section className="section sectionAlt">
      <div className="container">
        <div className="row">
          <div className="col col--6">
            <Heading as="h2">Easy Configuration</Heading>
            <p>
              Configure KafkaBus through <code>appsettings.json</code> with
              sensible defaults. Override settings per environment or per
              message type.
            </p>
            <ul>
              <li>Environment-specific configuration</li>
              <li>Custom configuration sections</li>
              <li>Per-message type overrides</li>
              <li>Full Confluent.Kafka settings support</li>
            </ul>
            <Link className="button button--primary" to="/docs/quick-start">
              Learn More →
            </Link>
          </div>
          <div className="col col--6">
            <CodeBlock language="json">{code}</CodeBlock>
          </div>
        </div>
      </div>
    </section>
  );
}

function MiddlewareExample() {
  const code = `[MiddlewareOrder(1)]
public class LoggingMiddleware<TKey, TMessage>(
    ILogger<LoggingMiddleware<TKey, TMessage>> logger) 
    : IConsumeMiddleware<TKey, TMessage>
{
    public async Task InvokeAsync(
        ConsumeContext<TKey, TMessage> context,
        ConsumeDelegate<TKey, TMessage> next,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Processing {Topic}", context.Topic);

        await next(context, ct);

        logger.LogInformation("Processed in {Elapsed}ms", sw.ElapsedMilliseconds);
    }
}`;

  return (
    <section className="section">
      <div className="container">
        <div className="row">
          <div className="col col--6">
            <CodeBlock language="csharp">{code}</CodeBlock>
          </div>
          <div className="col col--6">
            <Heading as="h2">Powerful Middleware</Heading>
            <p>
              Intercept messages with the middleware pipeline. Add cross-cutting
              concerns without modifying your business logic.
            </p>
            <ul>
              <li>Logging & Metrics</li>
              <li>Retry & Circuit Breaker</li>
              <li>Validation & Enrichment</li>
              <li>Distributed Tracing</li>
              <li>Custom ordering with attributes</li>
            </ul>
            <Link
              className="button button--primary"
              to="/docs/producer/middleware"
            >
              Learn More →
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}

function CTASection() {
  return (
    <section className="section sectionAlt">
      <div className="container text--center">
        <Heading as="h2">Ready to Get Started?</Heading>
        <p className="margin-bottom--lg">
          Add KafkaBus to your project and start building reliable Kafka
          applications in minutes.
        </p>
        <div className="buttons">
          <Link className="button button--primary button--lg" to="/docs">
            Read the Docs
          </Link>
          <Link
            className="button button--outline button--lg margin-left--md"
            to="https://www.nuget.org/packages/KafkaBus"
          >
            View on NuGet
          </Link>
        </div>
      </div>
    </section>
  );
}

export default function Home() {
  const { siteConfig } = useDocusaurusContext();
  return (
    <Layout title={siteConfig.title} description={siteConfig.tagline}>
      <HeroSection />
      <main>
        <InstallSection />
        <FeaturesSection />
        <CodeExamplesSection />
        <ConfigExample />
        <MiddlewareExample />
        <CTASection />
      </main>
    </Layout>
  );
}
