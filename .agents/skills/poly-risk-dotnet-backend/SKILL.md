---
name: poly-risk-dotnet-backend
description: Use when diagnosing or changing PolyRisk's .NET lobby/server reliability, throughput, latency, or resource use. Apply measured ASP.NET Core practices to the existing server without changing game rules or replacing its architecture.
---

# PolyRisk .NET backend

Start with `Server/PolyRisk.LobbyServer/README.md`, the existing validation harness, and the exact current target framework. Use [davidfowl/AspNetCoreDiagnosticScenarios](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios) and [ASP.NET Core best practices](https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/fundamentals/best-practices.md) as review references, not as instructions to add infrastructure.

## Workflow

1. Identify the user-visible failure or measured bottleneck. Record the baseline before changing code.
2. Trace the relevant request, state transition, persistence, and client retry behavior. Preserve authoritative validation, six-seat capacity, command idempotence, reconnect/recovery, and host handoff.
3. Review hot paths for blocking I/O, sync-over-async, unnecessary allocations, unbounded work, and contention. Use async I/O when the API supports it; do not wrap synchronous work in `Task.Run` as a substitute.
4. Make the smallest change that addresses evidence. Do not add caching, queues, databases, SignalR, Nakama, or other infrastructure without a demonstrated need.
5. Run the existing focused smoke/regression checks, then persistence/restart and multi-client protocol checks when relevant. Compare latency, throughput, CPU, and memory under a repeatable load before claiming an optimization.
6. Keep QA servers/data isolated from any shared preview or user game. Report which checks ran and the remaining limits.

`dotnet/crank` is a possible later benchmark tool; for the current small turn-based lobby, first use a simple repeatable load test against the existing harness. Avoid optimizing for hypothetical scale beyond the requested six players per game without measurements.

