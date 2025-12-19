namespace KafkaBus.Domain.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class MiddlewareOrderAttribute(int order) : Attribute
{
    public int Order { get; } = order;
}
