$ErrorActionPreference = "Stop"

$publishDir = Join-Path $PSScriptRoot "publish"

Write-Host "Publishing service to $publishDir"
dotnet publish -c Release -r win-x64 --self-contained false -o $publishDir

$exePath = Join-Path $publishDir "FasThinkQueueWorkerService.exe"
if (-not (Test-Path $exePath)) {
    throw "Published executable not found at $exePath"
}

Write-Host "Creating Windows service FasThinkQueueWorkerService"
sc.exe create FasThinkQueueWorkerService binPath= "\"$exePath\"" start= auto

Write-Host "Configuring failure actions"
sc.exe failure FasThinkQueueWorkerService reset= 86400 actions= restart/60000/restart/60000/restart/60000

Write-Host "Starting service"
sc.exe start FasThinkQueueWorkerService
