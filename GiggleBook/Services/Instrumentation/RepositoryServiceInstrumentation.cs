using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GiggleBook.Services.Instrumentation;

public class RepositoryServiceInstrumentation : IDisposable
{
    public ActivitySource ActivitySource { get; } = new ActivitySource(ActivitySourceName);
    public static Meter Meter { get; } = new Meter(MeterName);
    public static Counter<long> SuccessfulWritesCounter { get; } = Meter.CreateCounter<long>("successful_writes_total", "Total number of successful writes");
    public static Counter<long> FailedWritesCounter { get; } = Meter.CreateCounter<long>("failed_writes_total", "Total number of failed writes");
    public Histogram<long> WriteExecutionTimeHistogram { get; } = Meter.CreateHistogram<long>("xecution_time_seconds", "Histogram for the execution time of write operation");

    internal const string ActivitySourceName = nameof(RepositoryService);
    internal const string MeterName = nameof(RepositoryService);
    public RepositoryServiceInstrumentation()
    { }

    public void Dispose()
    {
        ActivitySource.Dispose();
    }
}
