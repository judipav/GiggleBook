using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GiggleBook.Services.Instrumentation;

public class RepositoryServiceInstrumentation : IDisposable
{
    public ActivitySource ActivitySource { get; } = new ActivitySource(ActivitySourceName);
    public Meter Meter { get; } 
    public Counter<long> SuccessfulWritesCounter { get; }
    public Counter<long> FailedWritesCounter { get; }
   
    internal const string ActivitySourceName = nameof(RepositoryService);
    internal const string MeterName = nameof(RepositoryService);
    public RepositoryServiceInstrumentation()
    { 
        Meter = new Meter(MeterName);
        SuccessfulWritesCounter = Meter.CreateCounter<long>("successful_writes_total", unit: "trn", description: "Total number of successful writes");
        FailedWritesCounter = Meter.CreateCounter<long>("failed_writes_total", unit: "trn", description: "Total number of failed writes");
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
    }
}
