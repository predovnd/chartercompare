# Google OAuth Setup for Azure Deployment

This guide explains how to configure Google OAuth authentication for your Azure-deployed application.

## Step 1: Create Google OAuth Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Navigate to **APIs & Services** > **Credentials**
4. Click **Create Credentials** > **OAuth client ID**
5. If prompted, configure the OAuth consent screen:
   - Choose **External** (unless you have a Google Workspace)
   - Fill in required fields (App name, User support email, Developer contact)
   - Add your email to test users if needed
   - Click **Save and Continue** through the steps
6. Select **Web application** as the application type
7. **Name**: Give it a name (e.g., "CharterCompare Production")
8. **Authorized redirect URIs**: Add your Azure App Service callback URL:
   ```
   https://your-app-name.azurewebsites.net/api/auth/google-callback
   ```
   Replace `your-app-name` with your actual Azure App Service name.
   
   **Example:**
   ```
   https://chartercompare-dev-b2b9hrctajeah9ef.australiacentral-01.azurewebsites.net/api/auth/google-callback
   ```
   
   **Important:**
   - Use `https://` (not `http://`)
   - Include the full domain (including region suffix if present)
   - No trailing slashes
   - The path must be exactly `/api/auth/google-callback`
9. Click **Create**
10. Copy the **Client ID** and **Client Secret** (you'll need these in Step 2)

## Step 2: Configure Azure App Service

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your App Service (e.g., `chartercompare-dev-b2b9hrctajeah9ef`)
3. Go to **Configuration** → **Application settings**
4. Click **+ New application setting** and add these settings:

### Required Settings:

| Setting Name | Value | Example |
|-------------|-------|---------|
| `Authentication:Google:ClientId` | Your Google Client ID | `123456789-abc.apps.googleusercontent.com` |
| `Authentication:Google:ClientSecret` | Your Google Client Secret | `GOCSPX-xxxxxxxxxxxxx` |
| `Frontend:Url` | Your App Service URL | `https://chartercompare-dev-b2b9hrctajeah9ef.australiacentral-01.azurewebsites.net` |

### Setting Format:

The setting names use colons (`:`) which Azure will convert to nested configuration:
- `Authentication:Google:ClientId` → `Configuration["Authentication:Google:ClientId"]`
- `Authentication:Google:ClientSecret` → `Configuration["Authentication:Google:ClientSecret"]`
- `Frontend:Url` → `Configuration["Frontend:Url"]`

5. Click **Save** at the top
6. Azure will restart your app automatically

## Step 3: Verify Configuration

1. Wait for the app to restart (check **Deployment Center** → **Logs**)
2. Try to log in using Google OAuth
3. Check the **Log stream** in Azure Portal for any errors

## Troubleshooting

### Error: "Google OAuth is not configured"

**Cause:** The application settings are not configured or the app hasn't restarted.

**Solution:**
1. Verify settings are saved in Azure Portal
2. Check that setting names use colons (`:`) not dots (`.`)
3. Restart the App Service manually if needed
4. Check **Log stream** for configuration errors

### Error: "redirect_uri_mismatch"

**Cause:** The redirect URI in Google OAuth doesn't match your Azure URL.

**Solution:**
1. Go to Google Cloud Console → **Credentials**
2. Edit your OAuth client
3. Add the exact redirect URI:
   ```
   https://your-exact-azure-url.azurewebsites.net/api/auth/google-callback
   ```
4. Make sure it matches exactly (including `https://`, no trailing slash)

### Site Safety Warning

If you see a browser security warning:

1. **HTTPS Required**: Azure App Services use HTTPS by default. Make sure you're accessing via `https://`
2. **SSL Certificate**: Azure provides a free SSL certificate for `*.azurewebsites.net` domains
3. **Custom Domain**: If using a custom domain, you need to configure SSL in Azure
4. **Browser Warning**: Some browsers may show a warning for new sites - this is normal and will go away as the site gains trust

### Testing Locally

For local development, you'll also need to add:
```
http://localhost:5000/api/auth/google-callback
```
to your Google OAuth authorized redirect URIs.

## Security Best Practices

1. **Never commit secrets to Git**: Use Azure Application Settings or Key Vault
2. **Use different OAuth clients** for dev/staging/prod environments
3. **Rotate secrets regularly**: Update Client Secret in both Google and Azure
4. **Monitor usage**: Check Google Cloud Console for unusual activity

## Next Steps

After configuring OAuth:
- Test login with Google
- Verify users are created in your database
- Check that authentication cookies are working
- Test logout functionality
