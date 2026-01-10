# Charter Compare API

A .NET 8.0 Web API backend for the Charter Compare application.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Visual Studio 2022, VS Code, or Rider (optional but recommended)

## Getting Started

### 1. Navigate to the API project

```bash
cd server/CharterCompare.Api
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Run the application

```bash
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger` (in Development mode)

## API Endpoints

### Start Chat
```
POST /api/chat/start
Content-Type: application/json

{}
```

**Response:**
```json
{
  "sessionId": "session_1234567890_abc123",
  "replyText": "Hey — it's Alex from Charter Compare...",
  "icon": "Calendar"
}
```

### Send Message
```
POST /api/chat/message
Content-Type: application/json

{
  "sessionId": "session_1234567890_abc123",
  "text": "wedding"
}
```

**Response:**
```json
{
  "replyText": "About how many passengers will be travelling?",
  "isComplete": false,
  "finalPayload": null,
  "icon": "Users"
}
```

## Configuration

### CORS

The API is configured to allow requests from:
- `http://localhost:5173` (Vite default)
- `http://localhost:3000` (alternative frontend port)

To modify CORS settings, edit `Program.cs` or `appsettings.json`.

### JSON File Storage

When a chat request is completed, the JSON payload is:
1. Saved to the SQLite database (`chartercompare.db`)
2. Also written to a file in the `requests/` directory (backup)

**File naming format:**
```
charter-request_YYYYMMDD_HHMMSS_email_at_domain_com.json
```

**Example:**
```
charter-request_20260108_143022_user_at_example_com.json
```

Files are stored in: `server/CharterCompare.Api/requests/`

**Note:** The `requests/` directory and database file are excluded from git via `.gitignore` to prevent committing sensitive customer data.

### Provider Portal

The API includes a provider portal where service providers can:
- Sign in with Google OAuth
- View open charter requests
- Submit quotes for requests

See `PROVIDER_SETUP.md` for detailed setup instructions.

### Session Storage

Currently using in-memory cache (IMemoryCache) for session storage. This is suitable for local development but should be replaced with:
- **Azure Redis Cache** for production
- **Azure Cosmos DB** for persistent storage
- **SQL Server** for relational data

## Development

### Project Structure

```
CharterCompare.Api/
├── Controllers/          # API controllers
├── Models/              # Data models (DTOs)
├── Services/            # Business logic
├── Properties/          # Launch settings
└── Program.cs           # Application entry point
```

### Adding New Features

1. Add models in `Models/`
2. Implement business logic in `Services/`
3. Create controllers in `Controllers/`
4. Register services in `Program.cs`

## Deployment to Azure

### Option 1: Azure App Service

1. Create an Azure App Service (Linux, .NET 8)
2. Configure deployment from GitHub/DevOps
3. Set environment variables in Azure Portal
4. Enable Application Insights for monitoring

### Option 2: Azure Container Apps

1. Create a Dockerfile (see below)
2. Build and push to Azure Container Registry
3. Deploy to Azure Container Apps

### Dockerfile Example

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CharterCompare.Api/CharterCompare.Api.csproj", "CharterCompare.Api/"]
RUN dotnet restore "CharterCompare.Api/CharterCompare.Api.csproj"
COPY . .
WORKDIR "/src/CharterCompare.Api"
RUN dotnet build "CharterCompare.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CharterCompare.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CharterCompare.Api.dll"]
```

## Next Steps

- [ ] Add database persistence (Azure Cosmos DB or SQL Server)
- [ ] Implement authentication/authorization
- [ ] Add logging and monitoring (Application Insights)
- [ ] Set up CI/CD pipeline
- [ ] Add unit and integration tests
- [ ] Configure Azure Key Vault for secrets
- [ ] Add rate limiting
- [ ] Implement email/SMS notifications

## Troubleshooting

### Port Already in Use

If port 5000 or 5001 is already in use, modify `Properties/launchSettings.json` to use different ports.

### CORS Issues

Ensure the frontend URL matches the CORS configuration in `Program.cs`.

### Session Not Found

Sessions are stored in memory and will be lost on application restart. Consider using persistent storage for production.
