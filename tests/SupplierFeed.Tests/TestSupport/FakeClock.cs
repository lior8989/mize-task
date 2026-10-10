using SupplierFeed.Api.Infrastructure;

namespace SupplierFeed.Tests.TestSupport;

/// <summary>
/// A manually advanced <see cref="IClock"/> for deterministic time-based tests.
/// </summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset start)
    {
        UtcNow = start;
    }

    public DateTimeOffset UtcNow { get; private set; }

    public void Advance(TimeSpan by) => UtcNow += by;
}
