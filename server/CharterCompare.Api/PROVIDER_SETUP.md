# Provider Portal Setup Guide

## Google OAuth Configuration

### 1. Create Google OAuth Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Navigate to **APIs & Services** > **Credentials**
4. Click **Create Credentials** > **OAuth client ID**
5. Configure the OAuth consent screen if prompted
6. Select **Web application** as the application type
7. Add authorized redirect URIs (you need to add ALL of these):
   - `http://localhost:5000/api/auth/google-callback` (HTTP - most common)
   - `https://localhost:5001/api/auth/google-callback` (HTTPS)
   
   **Important:** 
   - The redirect URI must EXACTLY match (including http vs https and the port number)
   - No trailing slashes
   - Check your backend terminal output to see which URL it's using (http://localhost:5000 or https://localhost:5001)
   - If you see "Now listening on: http://localhost:5000" use the HTTP version
   - If you see "Now listening on: https://localhost:5001" use the HTTPS version
8. Copy the **Client ID** and **Client Secret**

### 2. Configure Backend

Update `appsettings.Development.json`:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-google-client-secret"
    }
  }
}
```

For production, use Azure Key Vault or environment variables.

## Provider Portal Features

### Authentication
- **Login**: `/api/auth/google` - Initiates Google OAuth flow
- **Callback**: `/api/auth/google-callback` - Handles OAuth callback
- **Current User**: `GET /api/auth/me` - Returns current authenticated provider
- **Logout**: `POST /api/auth/logout` - Logs out the provider

### Provider Endpoints (Requires Authentication)
- **Get Requests**: `GET /api/provider/requests` - List all open requests
- **Get Request Details**: `GET /api/provider/requests/{id}` - Get specific request with quotes
- **Submit Quote**: `POST /api/provider/requests/{requestId}/quotes` - Submit a quote for a request
- **My Quotes**: `GET /api/provider/quotes` - Get all quotes submitted by the provider

## Frontend Routes

- `/provider/login` - Provider login page
- `/provider/dashboard` - Provider dashboard (requires authentication)

## Database

The system uses SQLite for local development. The database file `chartercompare.db` will be created automatically.

**Tables:**
- `Providers` - Provider accounts
- `CharterRequests` - Customer requests
- `Quotes` - Quotes submitted by providers

## Testing

1. Configure Google OAuth credentials
2. Start the backend: `cd server/CharterCompare.Api && dotnet run`
3. Navigate to `http://localhost:5173/provider/login`
4. Click "Sign in with Google"
5. Complete Google authentication
6. You'll be redirected to the dashboard where you can view requests and submit quotes
