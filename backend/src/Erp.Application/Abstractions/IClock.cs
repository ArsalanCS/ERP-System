namespace Erp.Application.Abstractions;

/// <summary>Abstraction over the system clock so time can be controlled in tests.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
