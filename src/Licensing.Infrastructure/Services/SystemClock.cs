using Licensing.Application.Abstractions;

namespace Licensing.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
