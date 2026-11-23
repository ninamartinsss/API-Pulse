using ApiPulse;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<MonitorStore>();
builder.Services.AddHttpClient("probe", client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddHostedService<MonitorWorker>();
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/targets", (MonitorStore store) => Results.Ok(store.Targets));
app.MapPost("/api/targets", (TargetInput input, MonitorStore store) =>
{
    if (string.IsNullOrWhiteSpace(input.Name) || !Uri.TryCreate(input.Url, UriKind.Absolute, out var uri) ||
        (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        return Results.BadRequest(new { error = "Name and valid HTTP(S) URL are required" });
    var target = new MonitorTarget(Guid.NewGuid(), input.Name.Trim(), uri.ToString());
    store.Add(target);
    return Results.Created($"/api/targets/{target.Id}", target);
});
app.MapDelete("/api/targets/{id:guid}", (Guid id, MonitorStore store) => store.Remove(id) ? Results.NoContent() : Results.NotFound());
app.MapGet("/api/results", (MonitorStore store) => Results.Ok(store.Results));
app.MapGet("/api/metrics", (MonitorStore store) => Results.Ok(store.GetMetrics()));
app.MapPost("/api/check/{id:guid}", async (Guid id, MonitorStore store, IHttpClientFactory clients, CancellationToken ct) =>
{
    var target = store.Targets.FirstOrDefault(x => x.Id == id);
    if (target is null) return Results.NotFound();
    var result = await Probe.CheckAsync(target, clients.CreateClient("probe"), ct);
    store.Record(result);
    return Results.Ok(result);
});
app.Run();
