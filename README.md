# API Pulse

A compact **C# / ASP.NET Core 8** HTTP uptime and metrics monitor, built as a software engineering portfolio project.

## Features
- Add, list and remove monitored HTTP(S) endpoints
- Background checks every 60 seconds and optional on-demand checks
- Rolling in-memory results (1,000 samples)
- Per-target uptime percentage and average latency
- xUnit tests, Docker container and GitHub Actions CI

## Run
Requires .NET 8 SDK.

```bash
dotnet run --project src/ApiPulse --urls http://localhost:5080
```

## API
| Method | Endpoint | Action |
|---|---|---|
| GET | `/health` | Health check |
| GET | `/api/targets` | List targets |
| POST | `/api/targets` | Add monitoring target |
| DELETE | `/api/targets/{id}` | Remove target |
| POST | `/api/check/{id}` | Check now |
| GET | `/api/results` | Latest results |
| GET | `/api/metrics` | Aggregated metrics |

```bash
curl -X POST http://localhost:5080/api/targets -H "Content-Type: application/json" -d '{"name":"Example","url":"https://example.com"}'
curl http://localhost:5080/api/metrics
```

## Tests
```bash
dotnet test tests/ApiPulse.Tests/ApiPulse.Tests.csproj
```

## Docker
```bash
docker build -t api-pulse .
docker run --rm -p 8080:8080 api-pulse
```

## Security and limitations
MVP data is stored **in memory** and is lost when the application restarts. No authentication is implemented. Because arbitrary target URLs can access internal network resources, do not expose this API publicly or accept untrusted targets without adding authorization, SSRF defenses and outbound network restrictions.

## License
MIT.
