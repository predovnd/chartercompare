#!/bin/bash
# EF Core Migration Helper Script
# Usage: ./migrate.sh [command] [migration-name]
# Commands: add, update, list, remove

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRASTRUCTURE_PROJECT="$SCRIPT_DIR/CharterCompare.Infrastructure/CharterCompare.Infrastructure.csproj"
API_PROJECT="$SCRIPT_DIR/CharterCompare.Api/CharterCompare.Api.csproj"

if [ ! -f "$INFRASTRUCTURE_PROJECT" ]; then
    echo "Error: Infrastructure project not found at $INFRASTRUCTURE_PROJECT"
    exit 1
fi

if [ ! -f "$API_PROJECT" ]; then
    echo "Error: API project not found at $API_PROJECT"
    exit 1
fi

COMMAND="${1:-help}"
MIGRATION_NAME="${2:-}"

case "$COMMAND" in
    add)
        if [ -z "$MIGRATION_NAME" ]; then
            echo "Error: Migration name is required for 'add' command"
            echo "Usage: ./migrate.sh add <MigrationName>"
            exit 1
        fi
        echo "Creating migration: $MIGRATION_NAME"
        dotnet ef migrations add "$MIGRATION_NAME" \
            --project "$INFRASTRUCTURE_PROJECT" \
            --startup-project "$API_PROJECT"
        ;;
    update)
        echo "Applying pending migrations..."
        dotnet ef database update \
            --project "$INFRASTRUCTURE_PROJECT" \
            --startup-project "$API_PROJECT"
        ;;
    list)
        echo "Listing migrations:"
        dotnet ef migrations list \
            --project "$INFRASTRUCTURE_PROJECT" \
            --startup-project "$API_PROJECT"
        ;;
    remove)
        if [ -z "$MIGRATION_NAME" ]; then
            echo "Error: Migration name is required for 'remove' command"
            echo "Usage: ./migrate.sh remove <MigrationName>"
            exit 1
        fi
        echo "Removing migration: $MIGRATION_NAME (if not applied)"
        dotnet ef migrations remove \
            --project "$INFRASTRUCTURE_PROJECT" \
            --startup-project "$API_PROJECT"
        ;;
    help|--help|-h)
        echo "EF Core Migration Helper"
        echo ""
        echo "Usage: ./migrate.sh [command] [migration-name]"
        echo ""
        echo "Commands:"
        echo "  add <name>      Create a new migration"
        echo "  update          Apply pending migrations to database"
        echo "  list            List all migrations and their status"
        echo "  remove          Remove the last migration (if not applied)"
        echo "  help            Show this help message"
        echo ""
        echo "Examples:"
        echo "  ./migrate.sh add AddEmailToUser"
        echo "  ./migrate.sh update"
        echo "  ./migrate.sh list"
        ;;
    *)
        echo "Error: Unknown command: $COMMAND"
        echo "Run './migrate.sh help' for usage information"
        exit 1
        ;;
esac
