#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Uninstalls MinRecall Windows Services

.DESCRIPTION
    This script stops and removes the MinRecall Capture and Optimizer services.
    Must be run as Administrator.

.EXAMPLE
    .\uninstall_services.ps1

.NOTES
    Author: MinRecall Team
    Version: 1.0.0
#>

# Set error action preference
$ErrorActionPreference = "Continue"

# Color output functions
function Write-Success { Write-Host "✓ $args" -ForegroundColor Green }
function Write-Info { Write-Host "ℹ $args" -ForegroundColor Cyan }
function Write-Warning { Write-Host "⚠ $args" -ForegroundColor Yellow }
function Write-Failure { Write-Host "✗ $args" -ForegroundColor Red }

# Header
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MinRecall Services Uninstallation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Failure "This script must be run as Administrator!"
    Write-Info "Right-click PowerShell and select 'Run as Administrator'"
    exit 1
}

# Function to check if service exists
function Test-ServiceExists {
    param([string]$ServiceName)
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    return $null -ne $service
}

# Function to remove service
function Remove-Service {
    param(
        [string]$ServiceName,
        [string]$DisplayName
    )
    
    Write-Host "Removing $DisplayName..." -ForegroundColor Yellow
    
    if (-not (Test-ServiceExists -ServiceName $ServiceName)) {
        Write-Warning "Service '$ServiceName' not found. It may already be uninstalled."
        return $false
    }
    
    try {
        # Get service status
        $service = Get-Service -Name $ServiceName -ErrorAction Stop
        Write-Info "Current status: $($service.Status)"
        
        # Stop service if running
        if ($service.Status -ne "Stopped") {
            Write-Info "Stopping service..."
            Stop-Service -Name $ServiceName -Force -ErrorAction Stop
            Start-Sleep -Seconds 2
            Write-Success "Service stopped"
        }
        else {
            Write-Info "Service is already stopped"
        }
        
        # Delete service
        Write-Info "Deleting service..."
        sc.exe delete $ServiceName | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Start-Sleep -Seconds 1
            Write-Success "$DisplayName removed successfully"
            return $true
        }
        else {
            Write-Failure "Failed to delete service. Exit code: $LASTEXITCODE"
            return $false
        }
    }
    catch {
        Write-Failure "Error removing $DisplayName : $_"
        return $false
    }
}

# Remove CaptureService
Write-Host ""
$captureRemoved = Remove-Service -ServiceName "MinRecallCapture" -DisplayName "Capture Service"

# Remove OptimizerService
Write-Host ""
$optimizerRemoved = Remove-Service -ServiceName "MinRecallOptimizer" -DisplayName "Optimizer Service"

# Check remaining services
Write-Host ""
Write-Host "Verifying removal..." -ForegroundColor Yellow

$remainingServices = @()
if (Test-ServiceExists -ServiceName "MinRecallCapture") {
    $remainingServices += "MinRecallCapture"
}
if (Test-ServiceExists -ServiceName "MinRecallOptimizer") {
    $remainingServices += "MinRecallOptimizer"
}

if ($remainingServices.Count -eq 0) {
    Write-Success "All MinRecall services have been removed"
}
else {
    Write-Warning "The following services still exist:"
    $remainingServices | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
    Write-Info "You may need to restart your computer to complete removal"
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Uninstallation Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($captureRemoved -and $optimizerRemoved) {
    Write-Success "All services uninstalled successfully!"
    $exitCode = 0
}
elseif ($captureRemoved -or $optimizerRemoved) {
    Write-Warning "Some services were uninstalled, but not all."
    $exitCode = 1
}
else {
    Write-Failure "No services were uninstalled."
    $exitCode = 1
}

Write-Host ""
Write-Info "Note: User data and logs are preserved at:"
Write-Host "  - Database: $env:APPDATA\MinRecall\"
Write-Host "  - Logs: $env:LOCALAPPDATA\MinRecall\Logs\"
Write-Host ""
Write-Info "To remove user data, manually delete the folders above."
Write-Host ""

exit $exitCode
