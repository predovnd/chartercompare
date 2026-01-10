# User Attributes System

## Overview

The user type system has been refactored from separate `OperatorType` and `RequesterType` fields to a flexible **UserAttribute** system that allows users to have multiple attributes.

## What Changed

### Database Schema
- **Removed**: `OperatorType` and `RequesterType` columns from `Users` table
- **Added**: `UserAttributes` table (many-to-many relationship)
  - `Id` (Primary Key)
  - `UserId` (Foreign Key to Users)
  - `AttributeType` (Enum: Bus, Airplane, Train, Boat, Individual, Business)
  - `CreatedAt`
  - Unique constraint on `(UserId, AttributeType)` to prevent duplicates

### Attribute Types
- **Operator Attributes**: Bus (1), Airplane (2), Train (3), Boat (4)
- **Requester Attributes**: Individual (10), Business (11)

### Defaults
- **New Operators**: Default to `Bus` attribute
- **New Requesters**: Default to `Individual` attribute
- **Only Admins**: Can change user attributes

### Validation Rules
- **Mutually Exclusive**: Individual and Business cannot both be assigned to the same user
- **Role-Based**: Operator attributes (Bus, Airplane, etc.) can only be assigned to operators/admins
- **Role-Based**: Requester attributes (Individual, Business) can only be assigned to requesters

## API Changes

### Admin: Update User Attributes
```json
PUT /api/admin/users/{userId}/attributes
{
  "attributes": [1, 2],  // Bus, Airplane
  "companyName": "Company Name"  // Optional, used when Business attribute is set
}
```

### Admin: Get Users
The `UserDto` now includes:
```json
{
  "id": 1,
  "email": "user@example.com",
  "attributes": ["Bus", "Airplane"]  // List of attribute types
}
```

## Migration

The migration system automatically:
1. Creates `UserAttributes` table if it doesn't exist
2. Migrates existing users from old schema
3. Sets default attributes:
   - Operators/Admins → `Bus`
   - Requesters → `Individual`

## Benefits

1. **Flexibility**: Users can have multiple operator types (e.g., Bus + Airplane)
2. **Extensibility**: Easy to add new attribute types
3. **Normalization**: Proper many-to-many relationship
4. **Future-Proof**: Supports complex scenarios (e.g., operator who is also a requester)

## Frontend Updates Needed

1. **Admin Dashboard - User List**:
   - Display user attributes as badges/tags
   - Show multiple attributes for each user

2. **Admin Dashboard - Edit User**:
   - Add interface to manage user attributes
   - Show checkboxes/select for available attributes
   - Validate mutually exclusive attributes (Individual/Business)
   - Show CompanyName field when Business is selected

3. **Operator Registration**:
   - Remove operator type selector (defaults to Bus)
   - Can be hidden since only admin can change

4. **Requester Registration**:
   - Remove requester type selector (defaults to Individual)
   - No CompanyName field needed
