# Admin Setup Guide

## Creating an Admin User

To create the first admin user, use the following API endpoint:

### Endpoint
```
POST /api/auth/admin/create-admin
```

### Request Body
```json
{
  "email": "admin@example.com",
  "password": "your-secure-password",
  "name": "Admin",
  "companyName": "Optional Company Name"
}
```

### Example using curl
```bash
curl -X POST http://localhost:5000/api/auth/admin/create-admin \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "password": "SecurePassword123!",
    "name": "Admin"
  }'
```

### Important Notes
- Only **one** admin can be created. If an admin already exists, the endpoint will return an error.
- The admin must use email/password authentication (not Google OAuth).
- After creating the admin, you can log in at `/admin/login`.

## Admin Dashboard Features

The admin dashboard provides:

1. **Overview Tab**: Statistics dashboard showing:
   - Total Operators
   - Total Requests
   - Open Requests
   - Total Quotes

2. **Users Tab**: View all operators and admins with:
   - User details (name, email, company)
   - User type (Admin, Operator, Requester)
   - Authentication method
   - Account status (active/inactive)
   - Creation date

3. **Requests Tab**: View all charter bus requests with:
   - Request details
   - Status
   - Associated quotes
   - Quote operators

## Accessing the Admin Portal

1. Navigate to `http://localhost:5173/admin/login`
2. Enter your admin email and password
3. You'll be redirected to the admin dashboard

## Security

- Admin routes are protected by authentication
- Only users with `IsAdmin = true` can access admin endpoints
- Regular operators cannot access admin functionality
