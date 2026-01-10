# SQL Server Firewall Configuration Guide

This guide explains how to configure the Azure SQL Database firewall to allow connections from Azure App Services and other Azure services.

## Why Configure the Firewall?

Azure SQL Database has a built-in firewall that blocks all connections by default for security. To allow your App Services to connect to your database, you need to explicitly allow Azure services.

## Method 1: Allow All Azure Services (Recommended)

This is the easiest and most common method for allowing Azure App Services to connect.

### Steps:

1. **Go to Azure Portal**
   - Navigate to [portal.azure.com](https://portal.azure.com)
   - Sign in with your Azure account

2. **Find Your SQL Server**
   - In the search bar at the top, type "SQL servers"
   - Click on **SQL servers** (not SQL databases)
   - Find and click on your SQL Server name (e.g., `chartercompare-server`)

3. **Open Networking Settings**
   - In the left sidebar menu, under **Security**, click **Networking**
   - You'll see firewall configuration options

4. **Enable Azure Services Access**
   - Under **Firewall rules**, find the toggle: **"Allow Azure services and resources to access this server"**
   - **Turn this ON** (toggle to **Yes** or **Enabled**)
   - This creates an automatic firewall rule that allows all Azure services

5. **Save Changes**
   - Click **Save** at the top of the page
   - Wait for the confirmation message

**What this does:**
- Allows any Azure service (App Services, Azure Functions, GitHub Actions runners, etc.) to connect
- This is secure because it only allows Azure services, not the entire internet
- No need to manage specific IP addresses

## Method 2: During Database Creation

If you're creating a new SQL database, you can enable this during setup:

1. **Create SQL Database**
   - Go to Azure Portal → **SQL databases** → **Create**
   - Fill in the basic details (subscription, resource group, database name, server)

2. **Go to Networking Tab**
   - Click **Next: Networking** (or click the **Networking** tab)
   - Under **Firewall rules**, you'll see: **"Allow Azure services and resources to access this server"**

3. **Enable the Checkbox**
   - Check the box next to **"Allow Azure services and resources to access this server"**
   - Continue with database creation

4. **Complete Creation**
   - Click **Review + create** → **Create**
   - The firewall rule will be automatically configured

## Method 3: Using Azure CLI

If you prefer command-line tools:

```bash
# Login to Azure
az login

# Set your variables
RESOURCE_GROUP="your-resource-group-name"
SERVER_NAME="your-server-name"  # e.g., chartercompare-server

# Create firewall rule to allow Azure services
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SERVER_NAME \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Verify the rule was created
az sql server firewall-rule list \
  --resource-group $RESOURCE_GROUP \
  --server $SERVER_NAME
```

**Note:** The IP range `0.0.0.0` to `0.0.0.0` is a special rule in Azure that means "allow all Azure services". This is different from allowing all IPs (`0.0.0.0` to `255.255.255.255`).

## Method 4: Allow Specific IP Addresses (Alternative)

If you want more granular control (not recommended for App Services):

1. **Get Your App Service Outbound IPs**
   - Go to your App Service → **Properties**
   - Note the **Outbound IP addresses** (there may be multiple)
   - Copy all of them

2. **Add Firewall Rules**
   - Go to SQL Server → **Networking** → **Firewall rules**
   - Click **+ Add client IPv4 address** for each IP, OR
   - Click **+ Add a firewall rule**
   - Enter a name (e.g., "AppService-Dev")
   - Enter the start and end IP address
   - Click **OK** → **Save**

**Note:** This method is more complex because:
- App Service outbound IPs can change
- You need to update rules if IPs change
- The "Allow Azure services" toggle is simpler and more reliable

## Verification

To verify the firewall is configured correctly:

### Test 1: Check Firewall Rules

1. Go to SQL Server → **Networking**
2. Under **Firewall rules**, you should see:
   - **AllowAzureServices** rule with `0.0.0.0` - `0.0.0.0`, OR
   - Toggle **"Allow Azure services and resources to access this server"** should be **ON**

### Test 2: Test Connection from App Service

1. Go to your App Service → **Console** (under Development Tools)
2. Try to connect using a simple command (if available), or
3. Check the App Service **Log stream** for connection errors
4. If you see "Cannot open server" or "firewall" errors, the rule isn't working

### Test 3: Test from Azure Portal Query Editor

1. Go to your SQL Database → **Query editor**
2. Sign in with your Entra ID credentials (the same account used as SQL admin)
3. If you can connect and run queries, the database is accessible
4. Note: Query editor uses your browser's IP, not Azure services IP
5. **Important:** Since this is Entra-only, you must sign in with an Entra ID account that has been granted access to the database

## Troubleshooting

### Issue: "Cannot open server" Error

**Symptoms:**
- App Service logs show: `A network-related or instance-specific error occurred while establishing a connection to SQL Server`
- Error mentions firewall or network access

**Solution:**
1. Verify "Allow Azure services" is enabled (Method 1)
2. Check that you're using the correct connection string
3. Ensure the SQL Server exists and is running
4. Verify the database name is correct

### Issue: Connection Works Locally But Not from App Service

**Cause:** Your local IP is allowed, but Azure services aren't.

**Solution:** Enable "Allow Azure services" as described in Method 1.

### Issue: GitHub Actions Migrations Fail

**Symptoms:** Deployment workflow fails when running `dotnet ef database update`

**Solution:**
1. Ensure "Allow Azure services" is enabled
2. GitHub Actions runners are Azure-hosted, so they should be covered
3. If still failing, check the connection string in GitHub secrets
4. Verify the connection string format is correct

### Issue: Connection Works Then Stops Working

**Possible Causes:**
1. App Service was scaled/restarted and got new outbound IPs (if using specific IP rules)
2. Firewall rule was accidentally deleted
3. SQL Server was recreated

**Solution:** Use "Allow Azure services" toggle instead of specific IP rules for reliability.

## Security Best Practices

1. **Use "Allow Azure services" toggle** instead of allowing all IPs (`0.0.0.0` to `255.255.255.255`)
2. **Keep firewall enabled** - never disable the firewall entirely
3. **Use Private Endpoints** for production (advanced, more secure)
4. **Entra ID authentication is required** - This deployment uses Entra-only authentication (SQL authentication is disabled)
5. **Use managed identities** for App Services instead of connection strings with credentials
6. **Review firewall rules regularly** to remove unused rules
7. **Use different databases** for dev/staging/prod environments (e.g., `chartercompare-db-dev`, `chartercompare-db-staging`, `chartercompare-db-prod`)
8. **Grant least privilege** - Only grant necessary database roles to managed identities and service principals

## Additional Resources

- [Azure SQL Database Firewall Rules](https://learn.microsoft.com/azure/azure-sql/database/firewall-configure)
- [Configure Azure SQL Database Firewall](https://learn.microsoft.com/azure/azure-sql/database/firewall-create-server-level-portal-quickstart)
- [Troubleshoot Azure SQL Database Connectivity](https://learn.microsoft.com/azure/azure-sql/database/troubleshoot-common-errors-issues)

## Quick Reference

**Fastest Way to Enable:**
1. Azure Portal → SQL servers → [Your Server] → Networking
2. Turn ON "Allow Azure services and resources to access this server"
3. Click Save

**CLI Command:**
```bash
az sql server firewall-rule create \
  --resource-group <resource-group> \
  --server <server-name> \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```
