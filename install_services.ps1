#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Installs MinRecall Windows Services

.DESCRIPTION
    This script installs the MinRecall Capture and Optimizer services as Windows Services.
    Must be run as Administrator.

.PARAMETER InstallPath
    The path where MinRecall executables are located.
    Default: C:\Program Files\MinRecall

.EXAMPLE
    .\install_services.ps1
    
.EXAMPLE
    .\install_services.ps1 -InstallPath "C:\Custom\Path"

.NOTES
    Author: MinRecall Team
    Version: 1.0.0
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$InstallPath = "$env:ProgramFiles\MinRecall"
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Color output functions
function Write-Success { Write-Host "✓ $args" -ForegroundColor Green }
function Write-Info { Write-Host "ℹ $args" -ForegroundColor Cyan }
function Write-Warning { Write-Host "⚠ $args" -ForegroundColor Yellow }
function Write-Failure { Write-Host "✗ $args" -ForegroundColor Red }

# Header
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MinRecall Services Installation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Failure "This script must be run as Administrator!"
    Write-Info "Right-click PowerShell and select 'Run as Administrator'"
    exit 1
}

Write-Info "Install path: $InstallPath"
Write-Host ""

# Check if executables exist
$captureServicePath = Join-Path $InstallPath "MinRecall.CaptureService.exe"
$optimizerServicePath = Join-Path $InstallPath "MinRecall.Optimizer.exe"

Write-Info "Checking for executables..."

if (-not (Test-Path $captureServicePath)) {
    Write-Failure "CaptureService executable not found!"
    Write-Info "Expected location: $captureServicePath"
    Write-Info "Please ensure the files are extracted to: $InstallPath"
    exit 1
}
Write-Success "Found CaptureService: $captureServicePath"

if (-not (Test-Path $optimizerServicePath)) {
    Write-Failure "Optimizer executable not found!"
    Write-Info "Expected location: $optimizerServicePath"
    Write-Info "Please ensure the files are extracted to: $InstallPath"
    exit 1
}
Write-Success "Found Optimizer: $optimizerServicePath"
Write-Host ""

# Function to check if service exists
function Test-ServiceExists {
    param([string]$ServiceName)
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    return $null -ne $service
}

# Function to remove existing service
function Remove-ExistingService {
    param([string]$ServiceName)
    
    Write-Info "Checking for existing service: $ServiceName"
    
    if (Test-ServiceExists -ServiceName $ServiceName) {
        Write-Warning "Service already exists. Stopping and removing..."
        
        try {
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
            
            sc.exe delete $ServiceName | Out-Null
            Start-Sleep -Seconds 2
            
            Write-Success "Removed existing service: $ServiceName"
        }
        catch {
            Write-Warning "Could not remove existing service: $_"
        }
    }
}

# Install CaptureService
try {
    Write-Host "Installing Capture Service..." -ForegroundColor Yellow
    Write-Host ""
    
    Remove-ExistingService -ServiceName "MinRecallCapture"
    
    Write-Info "Creating service..."
    New-Service -Name "MinRecallCapture" `
        -BinaryPathName $captureServicePath `
        -DisplayName "MinRecall Capture Service" `
        -Description "Captures screenshots for MinRecall application. This service continuously monitors and captures screenshots." `
        -StartupType Automatic `
        -ErrorAction Stop | Out-Null
    
    Write-Success "Service created"
    
    Write-Info "Starting service..."
    Start-Service -Name "MinRecallCapture" -ErrorAction Stop
    Start-Sleep -Seconds 2
    
    $service = Get-Service -Name "MinRecallCapture"
    if ($service.Status -eq "Running") {
        Write-Success "Capture Service installed and started successfully!"
    }
    else {
        Write-Warning "Service installed but not running. Status: $($service.Status)"
    }
}
catch {
    Write-Failure "Failed to install Capture Service: $_"
    Write-Info "Check the error message above for details."
    exit 1
}

Write-Host ""

# Install OptimizerService
try {
    Write-Host "Installing Optimizer Service..." -ForegroundColor Yellow
    Write-Host ""
    
    Remove-ExistingService -ServiceName "MinRecallOptimizer"
    
    Write-Info "Creating service..."
    New-Service -Name "MinRecallOptimizer" `
        -BinaryPathName $optimizerServicePath `
        -DisplayName "MinRecall Optimizer Service" `
        -Description "Processes and optimizes screenshots for MinRecall application. This service performs compression and OCR." `
        -StartupType Automatic `
        -ErrorAction Stop | Out-Null
    
    Write-Success "Service created"
    
    Write-Info "Starting service..."
    Start-Service -Name "MinRecallOptimizer" -ErrorAction Stop
    Start-Sleep -Seconds 2
    
    $service = Get-Service -Name "MinRecallOptimizer"
    if ($service.Status -eq "Running") {
        Write-Success "Optimizer Service installed and started successfully!"
    }
    else {
        Write-Warning "Service installed but not running. Status: $($service.Status)"
    }
}
catch {
    Write-Failure "Failed to install Optimizer Service: $_"
    Write-Info "Check the error message above for details."
    
    # Try to clean up CaptureService if OptimizerService failed
    Write-Warning "Attempting to remove CaptureService due to failed installation..."
    try {
        Stop-Service -Name "MinRecallCapture" -Force -ErrorAction SilentlyContinue
        sc.exe delete "MinRecallCapture" | Out-Null
    }
    catch {
        Write-Warning "Could not remove CaptureService: $_"
    }
    
    exit 1
}

# Display service status
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Service Status" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

try {
    Get-Service -Name "MinRecallCapture", "MinRecallOptimizer" | Format-Table -Property Name, DisplayName, Status, StartType -AutoSize
}
catch {
    Write-Warning "Could not retrieve service status: $_"
}

# Success message
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Installation Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Success "All MinRecall services installed successfully!"
Write-Host ""
Write-Info "To manage services:"
Write-Host "  - View status:  Get-Service MinRecallCapture, MinRecallOptimizer"
Write-Host "  - Stop service: Stop-Service MinRecallCapture"
Write-Host "  - Start service: Start-Service MinRecallCapture"
Write-Host "  - Remove service: sc.exe delete MinRecallCapture"
Write-Host ""
Write-Info "Services will start automatically on system boot."
Write-Host ""
Write-Info "Check logs at: $env:LOCALAPPDATA\MinRecall\Logs\"
Write-Host ""
