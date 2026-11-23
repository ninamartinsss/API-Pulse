using System.Collections.Concurrent;
using System.Diagnostics;
namespace ApiPulse;
public record TargetInput(string Name, string Url);
public record MonitorTarget(Guid Id, string Name, string Url);
public record ProbeResult(Guid TargetId, string Name, DateTimeOffset CheckedAt, bool Up, int? StatusCode, long LatencyMs, string? Error);
public record TargetMetrics(Guid TargetId, string Name, int Checks, double UptimePercent, double AverageLatencyMs);
public sealed class MonitorStore
{
    private readonly ConcurrentDictionary<Guid, MonitorTarget> _targets = new();
    private readonly ConcurrentQueue<ProbeResult> _results = new();
    public IReadOnlyList<MonitorTarget> Targets => _targets.Values.OrderBy(t => t.Name).ToArray();
    public IReadOnlyList<ProbeResult> Results => _results.Reverse().Take(200).ToArray();
    public void Add(MonitorTarget target) => _targets[target.Id] = target;
    public bool Remove(Guid id) => _targets.TryRemove(id, out _);
    public void Record(ProbeResult result)
    {
        _results.Enqueue(result);
        while (_results.Count > 1000) _results.TryDequeue(out _);
    }
    public IReadOnlyList<TargetMetrics> GetMetrics() => _results.ToArray().GroupBy(r => r.TargetId)
        .Select(g => new TargetMetrics(g.Key, g.Last().Name, g.Count(),
            Math.Round(g.Count(x => x.Up) * 100.0 / g.Count(), 2),
            Math.Round(g.Average(x => x.LatencyMs), 2))).ToArray();
}
public static class Probe
{
    public static async Task<ProbeResult> CheckAsync(MonitorTarget target, HttpClient client, CancellationToken ct = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await client.GetAsync(target.Url, HttpCompletionOption.ResponseHeadersRead, ct);
            return new(target.Id, target.Name, DateTimeOffset.UtcNow, response.IsSuccessStatusCode,
                (int)response.StatusCode, watch.ElapsedMilliseconds, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new(target.Id, target.Name, DateTimeOffset.UtcNow, false, null, watch.ElapsedMilliseconds, ex.Message);
        }
    }
}
public sealed class MonitorWorker(IServiceScopeFactory scopes, ILogger<MonitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        do
        {
            using var scope = scopes.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<MonitorStore>();
            var client = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("probe");
            foreach (var target in store.Targets)
            {
                try { store.Record(await Probe.CheckAsync(target, client, stoppingToken)); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception ex) { logger.LogWarning(ex, "Probe failed for {Name}", target.Name); }
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}