# Deployment Guide

This guide explains how to set up and use the CI/CD pipeline for deploying Charter Compare to Azure App Service.

## Overview

The pipeline automatically deploys:
- **Dev branch** → DEV App Service (automatic)
- **Staging branch** → PROD App Service Staging Slot (automatic)
- **Production** → PROD App Service Production Slot (manual approval + slot swap)

## Prerequisites

1. Azure subscription with appropriate permissions
2. GitHub repository with Actions enabled
3. Two Azure App Services created:
   - **DEV App Service** (for dev branch deployments)
   - **PROD App Service** (with a staging deployment slot)
4. Azure SQL databases created for each environment:
   - **DEV database**: `chartercompare-db-dev` (for dev environment)
   - **PROD database**: `chartercompare-db-prod` (for production/staging slots)
   - Use environment-specific database names following the pattern: `chartercompare-db-{env}`

## Step 1: Create Azure App Services

### Create DEV App Service

1. Go to Azure Portal → App Services → Create
2. Choose:
   - **Runtime stack**: .NET 8 (LTS)
   - **Operating System**: Linux (recommended) or Windows
   - **App Service Plan**: Create or select an existing plan
3. Click **Review + create** → **Create**
4. Note the App Service name (e.g., `chartercompare-dev`)

### Create PROD App Service with Staging Slot

1. Create the production App Service (same steps as above, e.g., `chartercompare-prod`)
2. After creation, go to the App Service → **Deployment slots** → **+ Add Slot**
3. Name it `staging`
4. Click **Add**

## Step 2: Configure GitHub Environments

GitHub Environments allow you to set secrets per environment and require approvals.

### Create Environments

1. Go to your GitHub repository
2. Navigate to **Settings** → **Environments**
3. Click **New environment** and create three environments:

#### Environment: `dev`
- **Environment name**: `dev`
- **Deployment branches**: Select "Selected branches" → Add `dev`
- No protection rules needed

#### Environment: `staging`
- **Environment name**: `staging`
- **Deployment branches**: Select "Selected branches" → Add `staging`
- No protection rules needed

#### Environment: `prod`
- **Environment name**: `prod`
- **Deployment branches**: Select "Selected branches" → Add `staging` (yes, staging branch deploys to prod environment)
- **Required reviewers**: Add reviewers who can approve production deployments
- **Wait timer**: Optional - set if you want a delay before deployment (recommended: 0 minutes for immediate approval)

## Step 3: Add Secrets to Environments

### Dev Environment Secrets

1. Go to **Settings** → **Environments** → Click on `dev`
2. Under **Environment secrets**, click **Add secret** and add:

| Secret Name | Description | How to Get |
|------------|-------------|------------|
| `AZURE_APP_NAME_DEV` | Name of your DEV App Service | Copy from Azure Portal (e.g., `chartercompare-dev`) |
| `AZURE_PUBLISH_PROFILE_DEV` | Publish profile for DEV App Service | See "Obtain Publish Profiles" below |

### Staging Environment Secrets

1. Go to **Settings** → **Environments** → Click on `staging`
2. Add secrets:

| Secret Name | Description | How to Get |
|------------|-------------|------------|
| `AZURE_APP_NAME_PROD` | Name of your PROD App Service | Copy from Azure Portal (e.g., `chartercompare-prod`) |
| `AZURE_PUBLISH_PROFILE_PROD` | Publish profile for PROD App Service **staging slot** | See "Obtain Publish Profiles" below |

**Important**: Use the publish profile for the **staging slot**, not production.

### Prod Environment Secrets

1. Go to **Settings** → **Environments** → Click on `prod`
2. Add secrets:

| Secret Name | Description | How to Get |
|------------|-------------|------------|
| `AZURE_APP_NAME_PROD` | Name of your PROD App Service | Same as staging (e.g., `chartercompare-prod`) |
| `AZURE_RESOURCE_GROUP_PROD` | Resource group name | Copy from Azure Portal (e.g., `rg-chartercompare-prod`) |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | See "Service Principal Setup" below |
| `AZURE_SP_TENANT_ID` | Service Principal tenant ID | See "Service Principal Setup" below |
| `AZURE_SP_CLIENT_ID` | Service Principal client ID | See "Service Principal Setup" below |
| `AZURE_SP_CLIENT_SECRET` | Service Principal client secret | See "Service Principal Setup" below |

## Step 4: Obtain Publish Profiles

Publish profiles are XML files that contain deployment credentials.

### For DEV App Service

1. Go to Azure Portal → Your DEV App Service
2. Click **Get publish profile** (top menu bar)
3. Download the `.PublishSettings` file
4. Open it in a text editor
5. Copy the **entire XML content**
6. Paste it as the value for `AZURE_PUBLISH_PROFILE_DEV` secret in GitHub

### For PROD App Service Staging Slot

1. Go to Azure Portal → Your PROD App Service
2. Go to **Deployment slots** → Click on `staging` slot
3. Click **Get publish profile** (top menu bar)
4. Download and copy the entire XML content
5. Paste it as the value for `AZURE_PUBLISH_PROFILE_PROD` secret in GitHub

**Important**: Make sure you're getting the publish profile from the **staging slot**, not the production slot.

## Step 5: Create Service Principal (for Slot Swap)

The production deployment requires a Service Principal with permissions to swap slots.

### Option A: Using Azure CLI

```bash
# Login to Azure
az login

# Set variables (replace with your values)
SUBSCRIPTION_ID="your-subscription-id"
RESOURCE_GROUP="rg-chartercompare-prod"
APP_NAME="chartercompare-prod"

# Create Service Principal with Contributor role on the resource group
az ad sp create-for-rbac \
  --name "github-actions-chartercompare" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

The command will output JSON like:
```json
{
  "clientId": "...",
  "clientSecret": "...",
  "subscriptionId": "...",
  "tenantId": "..."
}
```

Copy these values to the `prod` environment secrets:
- `AZURE_SP_CLIENT_ID` = `clientId`
- `AZURE_SP_CLIENT_SECRET` = `clientSecret`
- `AZURE_SUBSCRIPTION_ID` = `subscriptionId`
- `AZURE_SP_TENANT_ID` = `tenantId`

### Option B: Using Azure Portal

1. Go to Azure Portal → **Azure Active Directory** → **App registrations**
2. Click **New registration**
3. Name: `github-actions-chartercompare`
4. Click **Register**
5. Note the **Application (client) ID** and **Directory (tenant) ID**
6. Go to **Certificates & secrets** → **New client secret**
7. Copy the secret value (you'll only see it once)
8. Go to **Subscriptions** → Select your subscription → **Access control (IAM)**
9. Click **Add** → **Add role assignment**
10. Role: **Contributor**
11. Assign access to: **User, group, or service principal**
12. Select: `github-actions-chartercompare`
13. Click **Save**

Then add to GitHub secrets:
- `AZURE_SP_CLIENT_ID` = Application (client) ID
- `AZURE_SP_CLIENT_SECRET` = Client secret value
- `AZURE_SUBSCRIPTION_ID` = Subscription ID (found in Subscriptions overview)
- `AZURE_SP_TENANT_ID` = Directory (tenant) ID

## Step 6: Configure Azure App Service Settings

### Connection Strings

Configure your SQL Server connection string in Azure Portal. **Note:** Azure SQL is configured for Entra-only authentication.

1. Go to App Service → **Configuration** → **Connection strings**
2. Click **+ New connection string**
3. Name: `DefaultConnection`
4. Value: Your SQL Server connection string with Entra authentication:
   - For App Service managed identity: `Server=tcp:yourserver.database.windows.net,1433;Database=chartercompare-db-{env};Authentication=Active Directory Default;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;`
   - Replace `{env}` with your environment suffix (e.g., `dev`, `staging`, `prod`)
   - Examples: `chartercompare-db-dev`, `chartercompare-db-staging`, `chartercompare-db-prod`
   - Ensure the App Service managed identity has been granted access to the SQL database
5. Type: `SQLAzure` (for Azure SQL)
6. Click **OK** → **Save**

**Important:** 
- SQL authentication (username/password) is disabled for this deployment
- The App Service managed identity must be added as a user to the SQL database with appropriate permissions
- You can assign the managed identity to the database using Azure Portal or T-SQL commands

### App Settings

Add any other configuration values:

1. Go to **Configuration** → **Application settings**
2. Add settings like:
   - `Authentication:Google:ClientId`
   - `Authentication:Google:ClientSecret`
   - `Frontend:Url` (e.g., `https://chartercompare.azurewebsites.net`)
3. Click **Save**

**Important**: Set these for both the production slot and staging slot separately if needed, or use slot settings if they should differ.

## Step 7: Using the Pipeline

### Deploy to Dev

1. Push commits to the `dev` branch
2. GitHub Actions will automatically:
   - Run tests
   - Build client and server
   - Deploy to DEV App Service

### Deploy to Staging

1. Push commits to the `staging` branch
2. GitHub Actions will automatically:
   - Run tests
   - Build client and server
   - Deploy to PROD App Service **staging slot**

### Deploy to Production

1. After staging deployment completes, you'll see a notification
2. Go to **Actions** tab in GitHub
3. Find the workflow run for the staging deployment
4. The **promote-to-production** job will show "Waiting" status
5. Click on the job → **Review deployments**
6. Click **Approve and deploy** (or **Reject**)
7. Once approved, the workflow will:
   - Swap the staging slot to production slot
   - Production slot now has the new code

## Rollback Procedure

If something goes wrong in production, you can quickly rollback by swapping back:

### Option 1: Using Azure Portal

1. Go to Azure Portal → PROD App Service
2. Click **Deployment slots**
3. Click **Swap** (between staging and production)
4. This swaps them back (staging becomes production, production becomes staging)

### Option 2: Using Azure CLI

```bash
az webapp deployment slot swap \
  --resource-group rg-chartercompare-prod \
  --name chartercompare-prod \
  --slot staging \
  --target-slot production
```

### Option 3: Re-run Failed Workflow

1. If you want to deploy an older commit, merge it to staging
2. Push to staging branch
3. Approve the production deployment

## Troubleshooting

### Deployment Fails with "Bad Request"

- Check that the publish profile is correct and not expired
- Verify the App Service name matches the secret exactly
- Ensure the App Service exists and is running

### Slot Swap Fails

- Verify the Service Principal has Contributor role on the resource group
- Check that `AZURE_APP_NAME_PROD` matches the App Service name exactly
- Ensure `AZURE_RESOURCE_GROUP_PROD` matches the resource group name exactly

### App Doesn't Serve React Routes

- Verify `Program.cs` has static file serving configured (already included in repo)
- Check that `wwwroot` folder contains the client build
- Ensure `MapFallbackToFile("index.html")` is after `MapControllers()`

### Connection String Issues

- Verify connection string is set in Azure App Service Configuration
- Check that the connection string name matches what's in code (`DefaultConnection`)
- Ensure SQL Server firewall allows Azure services
- Verify connection string uses `Authentication=Active Directory Default` (for managed identity)
- Ensure App Service managed identity has been granted access to the SQL database
- Check that Entra ID authentication is enabled on the SQL Server (Entra-only mode)
- Verify the managed identity has appropriate database roles (e.g., db_owner, db_datareader, db_datawriter)

### Build Fails

- Check that Node.js version matches what's in `.github/workflows/deploy.yml`
- Verify `.NET SDK` version is correct
- Check GitHub Actions logs for specific error messages

## Best Practices

1. **Always test in staging first** before approving production
2. **Monitor staging slot** after deployment to staging branch
3. **Use slot settings** in Azure to have different configs for staging vs production
4. **Set up Application Insights** for monitoring
5. **Enable deployment logs** in Azure App Service for debugging
6. **Keep staging slot warm** - don't delete it, just swap back and forth

## Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Deployment Slots](https://docs.microsoft.com/azure/app-service/deploy-staging-slots)
