# Troubleshooting Connection Issues

## Frontend can't connect to backend

### Step 1: Verify Backend is Running
1. Check if backend is running on port 5000:
   ```powershell
   Test-NetConnection -ComputerName localhost -Port 5000
   ```

2. Test the API directly:
   ```powershell
   $body = @{} | ConvertTo-Json
   Invoke-RestMethod -Uri "http://localhost:5000/api/chat/start" -Method POST -ContentType "application/json" -Body $body
   ```

### Step 2: Check Browser Console
1. Open browser DevTools (F12)
2. Go to Console tab
3. Look for:
   - CORS errors (blocked by CORS policy)
   - Network errors (failed to fetch)
   - The API request URL being called

### Step 3: Check CORS Configuration
The backend CORS is configured to allow:
- `http://localhost:5173`
- `http://localhost:5174`
- `http://localhost:3000`
- `http://127.0.0.1:5173`
- `http://127.0.0.1:5174`
- `http://127.0.0.1:3000`

**Important:** CORS middleware must be BEFORE `UseHttpsRedirection()` in `Program.cs`

### Step 4: Verify Frontend API URL
Check the browser console for the log: `API_BASE_URL configured as: ...`

The frontend should be using: `http://localhost:5000`

### Step 5: Common Issues

**Issue: CORS Error**
- Solution: Restart backend after CORS changes
- Make sure CORS middleware is before other middleware

**Issue: "Failed to fetch"**
- Backend might not be running
- Check firewall/antivirus blocking port 5000
- Try accessing `http://localhost:5000/swagger` in browser

**Issue: Connection refused**
- Backend is not running
- Start backend: `cd backend/CharterCompare.Api && dotnet run`

**Issue: 404 Not Found**
- Check the API route is correct: `/api/chat/start`
- Verify controller is registered

### Step 6: Enable Detailed Logging
The frontend now logs:
- API Request URL
- API Response status
- API Errors

Check browser console for these logs to see exactly what's happening.
