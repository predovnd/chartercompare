# EF Core Migration Helper Script (PowerShell)
# Usage: .\migrate.ps1 [command] [migration-name]
# Commands: add, update, list, remove

param(
    [Parameter(Position=0)]
    [ValidateSet("add", "update", "list", "remove", "help")]
    [string]$Command = "help",
    
    [Parameter(Position=1)]
    [string]$MigrationName = ""
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$InfrastructureProject = Join-Path $ScriptDir "CharterCompare.Infrastructure\CharterCompare.Infrastructure.csproj"
$ApiProject = Join-Path $ScriptDir "CharterCompare.Api\CharterCompare.Api.csproj"

if (-not (Test-Path $InfrastructureProject)) {
    Write-Error "Infrastructure project not found at $InfrastructureProject"
    exit 1
}

if (-not (Test-Path $ApiProject)) {
    Write-Error "API project not found at $ApiProject"
    exit 1
}

function Show-Help {
    Write-Host "EF Core Migration Helper" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: .\migrate.ps1 [command] [migration-name]"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  add <name>      Create a new migration"
    Write-Host "  update          Apply pending migrations to database"
    Write-Host "  list            List all migrations and their status"
    Write-Host "  remove          Remove the last migration (if not applied)"
    Write-Host "  help            Show this help message"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\migrate.ps1 add AddEmailToUser"
    Write-Host "  .\migrate.ps1 update"
    Write-Host "  .\migrate.ps1 list"
}

switch ($Command) {
    "add" {
        if ([string]::IsNullOrWhiteSpace($MigrationName)) {
            Write-Error "Migration name is required for 'add' command"
            Write-Host "Usage: .\migrate.ps1 add <MigrationName>"
            exit 1
        }
        Write-Host "Creating migration: $MigrationName" -ForegroundColor Green
        dotnet ef migrations add $MigrationName `
            --project $InfrastructureProject `
            --startup-project $ApiProject
    }
    "update" {
        Write-Host "Applying pending migrations..." -ForegroundColor Green
        dotnet ef database update `
            --project $InfrastructureProject `
            --startup-project $ApiProject
    }
    "list" {
        Write-Host "Listing migrations:" -ForegroundColor Green
        dotnet ef migrations list `
            --project $InfrastructureProject `
            --startup-project $ApiProject
    }
    "remove" {
        Write-Host "Removing last migration (if not applied)..." -ForegroundColor Yellow
        dotnet ef migrations remove `
            --project $InfrastructureProject `
            --startup-project $ApiProject
    }
    "help" {
        Show-Help
    }
    default {
        Write-Error "Unknown command: $Command"
        Show-Help
        exit 1
    }
}
