using ApiPulse;
using Xunit;
namespace ApiPulse.Tests;
public class MonitoringTests
{
 [Fact] public void StoreAddsAndRemovesTargets()
 {
  var store = new MonitorStore();
  var target = new MonitorTarget(Guid.NewGuid(), "Example", "https://example.com");
  store.Add(target);
  Assert.Single(store.Targets);
  Assert.True(store.Remove(target.Id));
  Assert.Empty(store.Targets);
 }
 [Fact] public void MetricsComputeUptime()
 {
  var store = new MonitorStore();
  var id = Guid.NewGuid();
  store.Record(new ProbeResult(id,"API",DateTimeOffset.UtcNow,true,200,10,null));
  store.Record(new ProbeResult(id,"API",DateTimeOffset.UtcNow,false,500,30,"error"));
  var metric = Assert.Single(store.GetMetrics());
  Assert.Equal(50,metric.UptimePercent);
  Assert.Equal(20,metric.AverageLatencyMs);
 }
 [Fact] public async Task ProbeReturnsUpOnSuccessfulHttp()
 {
  var handler = new FakeHandler();
  using var client = new HttpClient(handler);
  var target = new MonitorTarget(Guid.NewGuid(),"Example","https://example.com");
  var result = await Probe.CheckAsync(target,client);
  Assert.True(result.Up);
  Assert.Equal(200,result.StatusCode);
 }
 private sealed class FakeHandler : HttpMessageHandler
 {
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct) =>
   Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
 }
}