# User Types Enhancement

## Overview
Added support for operator types and requester types to provide more granular user classification.

## Changes

### Operator Types
- **Enum**: `OperatorType`
  - `Bus = 0` (default, only supported type initially)
  - `Plane = 1` (for future use)
  - `Train = 2` (for future use)
  - `Boat = 3` (for future use)
  - `Other = 99` (for future use)

- **Default**: All new operators default to `Bus` type
- **Database**: Added nullable `OperatorType` column to `Users` table

### Requester Types
- **Enum**: `RequesterType`
  - `Individual = 0` (default)
  - `Business = 1`

- **Default**: All new requesters default to `Individual` type
- **Database**: Added nullable `RequesterType` column to `Users` table
- **Business Requesters**: Can optionally provide `CompanyName`

## API Changes

### Operator Registration
```json
POST /api/auth/register
{
  "email": "operator@example.com",
  "password": "password",
  "name": "Operator Name",
  "companyName": "Company Name",
  "operatorType": 0  // Optional, defaults to 0 (Bus)
}
```

### Requester Registration
```json
POST /api/auth/requester/register
{
  "email": "requester@example.com",
  "password": "password",
  "name": "Requester Name",
  "phone": "1234567890"
}
```
**Note**: All new requesters are automatically set to `Individual` type. Only admins can change a requester's type to `Business` via the admin endpoint.

### Admin: Update Requester Type
```json
PUT /api/admin/users/{userId}/type
{
  "requesterType": 1,  // 0 = Individual, 1 = Business
  "companyName": "Company Name"  // Required when setting to Business
}
```

## Database Migration

For existing databases, the migration script will:
- Set all existing operators to `OperatorType = Bus (0)`
- Set all existing requesters to `RequesterType = Individual (0)`

## Frontend Updates Needed

1. **Operator Registration Form**:
   - Add operator type selector (currently only "Bus" available)
   - Can be hidden for now since only Bus is supported

2. **Requester Registration Form**:
   - Remove requester type selector (all requesters default to Individual)
   - No `CompanyName` field needed during registration

3. **Admin Dashboard**:
   - Add ability to change requester type from Individual to Business
   - Show `CompanyName` field when changing to Business
   - Display operator type and requester type in user list

3. **Admin Dashboard**:
   - Display operator type and requester type in user list
   - Add filters for operator/requester types

## Future Enhancements

- Support for additional operator types (Plane, Train, Boat)
- Filtering requests by operator type
- Different quote workflows for different operator types
- Business requester features (invoicing, company accounts, etc.)
