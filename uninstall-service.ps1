$ErrorActionPreference = "Continue"

Write-Host "Stopping service FasThinkQueueWorkerService"
sc.exe stop FasThinkQueueWorkerService

Write-Host "Deleting service FasThinkQueueWorkerService"
sc.exe delete FasThinkQueueWorkerService
