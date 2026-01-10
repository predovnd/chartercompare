# Deployment Setup Checklist

This is a step-by-step checklist to get your deployment working. Follow each step in order.

**Note:** This guide is for dev-only deployment from the `dev` branch. The workflow automatically deploys when you push to the `dev` branch.

## ✅ Prerequisites Check

- [ ] Azure subscription with active account
- [ ] GitHub repository with Actions enabled
- [ ] Access to Azure Portal
- [ ] Azure SQL Database (will create separately)

## Step 1: Create Azure SQL Server and Database

**Important:** Create the SQL Server and database separately from the App Service.

### 1.1 Create SQL Server

- [ ] Go to Azure Portal → **SQL servers** → **Create**
- [ ] **Resource Group**: Create new or use existing (e.g., `rg-chartercompare`)
- [ ] **Server name**: e.g., `chartercompare-server` (must be globally unique)
- [ ] **Location**: Choose a region close to you
- [ ] **Authentication method**: Choose **Entra ID only** (SQL authentication is disabled)
- [ ] **SQL admin**: Select an Entra ID user or group to be the server administrator
- [ ] Click **Review + create** → **Create**
- [ ] Wait for deployment to complete
- [ ] Note the server name (e.g., `chartercompare-server.database.windows.net`)

**Important:** This deployment uses Entra ID (Azure AD) authentication only. SQL authentication with username/password is not available.

### 1.2 Create SQL Database

- [ ] Go to Azure Portal → **SQL databases** → **Create**
- [ ] **Resource Group**: Same as SQL Server (e.g., `rg-chartercompare`)
- [ ] **Database name**: e.g., `chartercompare-db-dev` (include environment suffix: `-dev`, `-staging`, `-prod`)
- [ ] **Server**: Select the SQL Server you just created
- [ ] **Want to use SQL elastic pool?**: No
- [ ] **Compute + storage**: Choose **Serverless** (free tier) or **Basic** (lowest paid tier)
- [ ] Click **Review + create** → **Create**
- [ ] Wait for deployment to complete
- [ ] Note the database name (e.g., `chartercompare-db-dev`)

**Note:** Use environment-specific database names (e.g., `chartercompare-db-dev`, `chartercompare-db-staging`, `chartercompare-db-prod`) to separate environments.

### 1.3 Configure SQL Server Firewall

- [ ] Go to Azure Portal → **SQL servers** → Click on your server
- [ ] In the left menu, click **Networking** (under Security)
- [ ] Under **Firewall rules**, turn ON **"Allow Azure services and resources to access this server"**
- [ ] Click **Save** at the top

**For detailed firewall configuration, see:** `docs/SQL_FIREWALL_SETUP.md`

### 1.4 Get Connection String

**For App Service with Managed Identity (Recommended):**

- [ ] Go to your SQL Database → **Connection strings** (under Settings)
- [ ] Select **ADO.NET** tab
- [ ] Use the connection string format for Entra authentication:
  - Format: `Server=tcp:yourserver.database.windows.net,1433;Database=chartercompare-db-dev;Authentication=Active Directory Default;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;`
  - Replace `chartercompare-db-dev` with your actual database name (e.g., `chartercompare-db-staging`, `chartercompare-db-prod`)
  - This uses the App Service's managed identity for authentication
- [ ] **Save this connection string** - you'll need it later

**For GitHub Actions (Service Principal):**

- [ ] If running migrations from GitHub Actions, you'll need a connection string with service principal:
  - Format: `Server=tcp:yourserver.database.windows.net,1433;Database=chartercompare-db-dev;Authentication=Active Directory Service Principal;User ID=<client-id>@<tenant-id>;Password=<client-secret>;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;`
  - Replace `chartercompare-db-dev` with your actual database name for the target environment
- [ ] Create a service principal with SQL database access (see additional setup steps)
- [ ] Ensure the service principal has appropriate permissions on the SQL database

**Note:** Since this is Entra-only, you cannot use SQL username/password authentication.

## Step 2: Create App Service

Create the App Service separately from the SQL resources. This will be your dev environment.

### 2.1 Create App Service

- [ ] Go to Azure Portal → **App Services** → **Create**
- [ ] **Resource Group**: Same as SQL Server or create new (e.g., `rg-chartercompare`)
- [ ] **Name**: e.g., `chartercompare-dev` (must be globally unique, lowercase, no spaces)
- [ ] **Publish**: Code
- [ ] **Runtime stack**: .NET 8 (LTS)
- [ ] **Operating System**: Linux (recommended) or Windows
- [ ] **Region**: Same region as your SQL Server (for better performance)
- [ ] **App Service Plan**: 
  - Click **Create new** if you don't have one
  - **Plan name**: e.g., `plan-chartercompare-dev`
  - **Sku and size**: Choose **Free F1** (free tier) or **Basic B1** (lowest paid tier)
  - Click **OK**
- [ ] Click **Review + create** → **Create**
- [ ] Wait for deployment to complete
- [ ] Note the App Service name (you'll need this for GitHub secrets)

## Step 3: Configure App Service Settings

### 3.1 Configure Connection String

- [ ] Go to your App Service → **Configuration** → **Connection strings**
- [ ] Click **+ New connection string**
- [ ] Name: `DefaultConnection` (exactly this name - matches your code)
- [ ] Value: Paste your SQL Server connection string from Step 1.4
- [ ] Type: `SQLAzure` (for Azure SQL Database)
- [ ] Click **OK** → **Save** (at the top)

**Important:** After clicking Save, Azure will restart your App Service. This is normal.

### 3.2 Configure Application Settings (Optional)

- [ ] Go to **Configuration** → **Application settings**
- [ ] Add these settings if you're using them:
  - [ ] `Authentication:Google:ClientId` (if using Google authentication)
  - [ ] `Authentication:Google:ClientSecret` (if using Google authentication)
  - [ ] `Frontend:Url` = Your App Service URL (e.g., `https://chartercompare-dev.azurewebsites.net`)
- [ ] Click **Save** (at the top)

## Step 4: Obtain Publish Profile

- [ ] Go to Azure Portal → Your App Service
- [ ] Click **Get publish profile** (top menu bar)
- [ ] Download the `.PublishSettings` file
- [ ] Open it in a text editor (Notepad, VS Code, etc.)
- [ ] **Copy the entire XML content** (you'll paste this in GitHub as a secret)

**Important:** Save this file securely - it contains deployment credentials.

## Step 5: Configure GitHub Secrets

You need to add secrets to GitHub so the deployment workflow can access your Azure resources.

### 5.1 Create Dev Environment (Recommended)

Using GitHub Environments helps organize secrets. This is optional but recommended.

1. Go to your GitHub repository
2. Navigate to **Settings** → **Environments**
3. Click **New environment**
4. **Environment name**: `dev`
5. **Deployment branches**: Select "Selected branches" → Add `dev`
6. Click **Configure environment**

### 5.2 Add Secrets to Dev Environment

- [ ] Go to **Settings** → **Environments** → Click on `dev`
- [ ] Under **Environment secrets**, click **Add secret**

Add these secrets:

| Secret Name | Value | Where to Get |
|------------|-------|--------------|
| `AZURE_APP_NAME` | Your App Service name (e.g., `chartercompare-dev`) | Azure Portal (Step 2.1) |
| `AZURE_PUBLISH_PROFILE` | Entire XML from publish profile | Step 4 |
| `AZURE_SQL_CONNECTION_STRING` | Your SQL connection string | Step 1.4 |

- [ ] Save each secret

### 5.3 Alternative: Repository-Level Secrets

If you prefer not to use environments, you can add secrets at the repository level:

- [ ] Go to **Settings** → **Secrets and variables** → **Actions**
- [ ] Click **New repository secret**

Add these secrets:

| Secret Name | Value | Where to Get |
|------------|-------|--------------|
| `AZURE_APP_NAME` | Your App Service name (e.g., `chartercompare-dev`) | Azure Portal |
| `AZURE_PUBLISH_PROFILE` | Entire XML from publish profile | Step 4 |
| `AZURE_SQL_CONNECTION_STRING` | Your SQL connection string | Step 1.4 |

- [ ] Save each secret

**Note:** The workflow is configured to use the `dev` environment. If you use repository-level secrets instead, you'll need to update the workflow file to remove the `environment: name: dev` line.

## Step 6: Deploy Your Application

### 6.1 Create and Push to Dev Branch

- [ ] Create a `dev` branch: `git checkout -b dev`
- [ ] Push to GitHub: `git push -u origin dev`
- [ ] Go to GitHub → **Actions** tab
- [ ] Watch the workflow run automatically

### 6.2 Verify Deployment

- [ ] Wait for workflow to complete successfully
- [ ] Go to Azure Portal → Your App Service
- [ ] Check **Deployment Center** → **Logs** for deployment status
- [ ] Visit your App Service URL (e.g., `https://chartercompare-dev.azurewebsites.net`) in browser
- [ ] Verify the app is running

### 6.3 Test Database Connection

- [ ] Check App Service → **Log stream** for errors
- [ ] Verify database migrations ran successfully (check workflow logs in GitHub Actions)
- [ ] Test the application functionality
- [ ] Check **Application Insights** (if enabled) for errors

### 6.4 Future Deployments

Every time you push to the `dev` branch, the workflow will automatically:
1. Build your application
2. Run database migrations
3. Deploy to your App Service

Just commit your changes and push:
```bash
git add .
git commit -m "Your commit message"
git push origin dev
```

## Troubleshooting

### Deployment Fails

**Check:**
- [ ] Verify all secrets are set correctly in GitHub (environment or repository level)
- [ ] Check that App Service name matches exactly (case-sensitive)
- [ ] Verify publish profile XML is complete and correct
- [ ] Check GitHub Actions logs for specific error messages
- [ ] Ensure App Service exists and is running in Azure Portal

### Database Connection Errors

**Check:**
- [ ] Connection string is set correctly in Azure App Service Configuration
- [ ] Connection string name is exactly `DefaultConnection` (matches your code)
- [ ] SQL Server firewall allows Azure services (see Step 1.3)
- [ ] Database exists and is accessible
- [ ] App Service managed identity has been granted access to the SQL database
- [ ] Connection string uses `Authentication=Active Directory Default` for managed identity
- [ ] Check App Service **Log stream** for connection errors
- [ ] Verify Entra ID authentication is enabled on the SQL Server

### Migrations Fail

**Check:**
- [ ] Connection string secret `AZURE_SQL_CONNECTION_STRING` is set in GitHub
- [ ] Connection string uses Entra authentication (Service Principal format)
- [ ] Service principal has appropriate permissions on the SQL database (e.g., db_owner or db_ddladmin)
- [ ] Service principal has been added as a user in the database with necessary roles
- [ ] Check GitHub Actions logs for migration errors
- [ ] SQL Server firewall allows Azure services (GitHub Actions runs on Azure-hosted runners)
- [ ] Verify Entra ID authentication is enabled on the SQL Server

### App Not Serving React Routes

**Check:**
- [ ] Client build is included in deployment package (check workflow logs)
- [ ] `wwwroot` folder contains client files
- [ ] Verify `MapFallbackToFile("index.html")` is in `Program.cs`
- [ ] Check App Service logs for routing errors

### Workflow Not Triggering

**Check:**
- [ ] Ensure you're pushing to `main`, `master`, or `dev` branch
- [ ] Verify `.github/workflows/deploy.yml` exists in your repository
- [ ] Check GitHub Actions is enabled for your repository (Settings → Actions → General)

## Next Steps

After successful deployment:

1. [ ] Set up Application Insights for monitoring
2. [ ] Configure custom domains (if needed)
3. [ ] Set up SSL certificates
4. [ ] Configure auto-scaling (if needed)
5. [ ] Set up backup schedules
6. [ ] Configure alerting/notifications
7. [ ] Document your deployment process for your team

## Quick Reference

### Connection String Format

For Azure SQL Database with Entra authentication (App Service Managed Identity):
```
Server=tcp:yourserver.database.windows.net,1433;Database=chartercompare-db-{env};Authentication=Active Directory Default;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;
```
Replace `{env}` with your environment suffix (e.g., `dev`, `staging`, `prod`).

For GitHub Actions (Service Principal):
```
Server=tcp:yourserver.database.windows.net,1433;Database=chartercompare-db-{env};Authentication=Active Directory Service Principal;User ID=<client-id>@<tenant-id>;Password=<client-secret>;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;
```
Replace `{env}` with your environment suffix (e.g., `dev`, `staging`, `prod`).

**Important Notes:**
- Azure SQL is configured for Entra-only authentication (SQL authentication is disabled)
- App Service uses managed identity (Authentication=Active Directory Default)
- For GitHub Actions, you must use a service principal with appropriate SQL permissions
- Ensure the App Service managed identity or service principal has the required database roles

### Required GitHub Secrets

**For dev environment:**
- `AZURE_APP_NAME` - Your App Service name (e.g., `chartercompare-dev`)
- `AZURE_PUBLISH_PROFILE` - Publish profile XML content
- `AZURE_SQL_CONNECTION_STRING` - Full SQL connection string

### Important URLs

- GitHub Actions: `https://github.com/your-org/your-repo/actions`
- Azure Portal: `https://portal.azure.com`
- Your App Service: `https://your-app-name.azurewebsites.net` (replace with your actual name)

### Deployment Branch

- `dev` branch → Automatically deploys to your App Service when pushed
