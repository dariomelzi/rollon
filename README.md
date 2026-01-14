# FasThinkQueueWorkerService

A .NET 8 Windows Service built from the Worker Service template. It runs a SQL queue processor loop and watches FasThinkConnect logs to restart the service if a known cancellation pattern appears.

## Build

```bash
dotnet restore

dotnet build -c Release
```

## Publish

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

## Install as Windows Service

Run PowerShell as Administrator:

```powershell
./install-service.ps1
```

## Uninstall

Run PowerShell as Administrator:

```powershell
./uninstall-service.ps1
```

## Configuration

Configuration is loaded from `appsettings.json` and environment variables. The service uses SQL Authentication by default.

Key sections:

- `Sql:Server`, `Sql:Database`, `Sql:User`, `Sql:Password`, `Sql:ProcedureName`, `Sql:CommandTimeoutSeconds`
- `Worker:WorkerMaxSeconds`, `Worker:LoopSleepSeconds`, `Worker:SleepOnEmptySeconds`
- `Worker:BatchSize`, `Worker:OpDelaySeconds`, `Worker:OpHoldSeconds`, `Worker:InterItemDelaySeconds`
- `Worker:AlignPollMs`, `Worker:AlignMaxWaitSeconds`, `Worker:AlignRequireMismatch`, `Worker:MaxCleanupRetries`
- `Watch:IntervalSeconds`, `Watch:LogFolder`, `Watch:ServiceName`, `Watch:StateFilePath`, `Watch:CooldownMinutes`

## Logging

- Rolling file log: `C:\Scripts\FasThinkQueueWorker.log` (daily, 14-day retention).
- Best-effort Windows Event Log sink.
