Generate the codebase for CharterCompare using CharterCompare.Phased.doc as the single source of truth.

SCOPE
- Implement Phase 0 + Phase 1 ONLY.
- Follow ALL “Global Architecture & Invariants”.
- Add Docker support for local development (SQL Server in Docker).
- Do NOT implement Stripe payments/Stripe Connect, geocoding/radius matching, or AI beyond simple rule-based badges placeholders.

TECH STACK (FIXED)
- Frontend: React + TypeScript + Vite, Wouter, TanStack Query, shadcn/ui + Radix, Tailwind, Framer Motion
- Backend: .NET 8 Web API (C#)
- DB: SQL Server (local dev via Docker container; also allow LocalDB/Express as optional)
- Target hosting: Azure (document only; local dev must not require Azure)

REPO STRUCTURE (MONOREPO)
- /frontend
- /backend
- /shared
- /docker (optional)
- docker-compose.yml at repo root
- CharterCompare.Phased.md (already present; do not remove)

PHASE 0 FEATURES (Verified Request Capture)
Customer:
- Request page collects:
  - service_type (fixed “bus”)
  - pickup_location_text
  - pickup_suburb_or_postcode
  - dropoff_location_text
  - dropoff_suburb_or_postcode
  - email (required)
- Submit shows “Check your email to verify” + resend verification button.

Backend:
- Entities: CustomerIdentity, CustomerRequest, VerificationToken
- Flow:
  - normalize email
  - upsert CustomerIdentity (Email + EmailNormalized UNIQUE)
  - create CustomerRequest with status SubmittedUnverified
  - create single-use VerificationToken (expires e.g. 24h)
  - send verification email
- Dev email sender MUST write emails to: /backend/dev-emails/ (file-based). Include the verification URL in the file.
- Verify endpoint marks token used, sets request status OpenVerified, sets CustomerIdentity.EmailVerifiedAt if null.

Admin:
- Minimal admin UI page listing requests (status, createdAt, pickup/dropoff suburb, masked email).
- Admin access can be simplest allowlist by email in config; for local dev allow a DEV flag to bypass auth, documented clearly in README and OFF by default.

PHASE 1 FEATURES (Providers & Quotes)
Provider:
- Provider entity:
  - BusinessName, ContactName, Email, Phone (min required)
  - Status: PendingApproval, Approved, Suspended
  - ServiceAreasSimple: allowed suburbs list OR text rules (simple; no geocoding)
- Provider portal UI:
  - list requests assigned/notified to provider
  - submit quote for a request: amount + currency + notes

Matching (Phase 1 simple):
- When a request becomes OpenVerified, match providers using simple rules (allowed suburbs list OR basic string match against pickup_suburb_or_postcode).
- Create ProviderNotification records (RequestId, ProviderId, NotifiedAt).
- Only Approved providers are matched/notified.

Customer (Phase 1):
- Quote comparison page for verified requests:
  - list all quotes with badges:
    - Cheapest
    - Fastest responder (earliest quote CreatedAt)
- No booking/payment yet.

DATA & ARCHITECTURE REQUIREMENTS
- Use SQL Server + EF Core migrations.
- Implement IStorage abstraction:
  - define IStorage interface
  - implement SqlStorage using EF Core DbContext
  - business logic must not depend directly on DbContext
- /shared:
  - Zod schemas for API contracts
  - Frontend uses @shared alias
  - Backend uses matching DTOs and validates requests server-side too
- TanStack Query used for all API calls.

MINIMUM API ENDPOINTS
Customer:
- POST /api/requests
- POST /api/requests/{id}/resend-verification
- GET  /api/requests/verify?token=...
- GET  /api/requests/{id}              (only if verified)
- GET  /api/requests/{id}/quotes       (only if verified)

Provider:
- GET  /api/provider/me
- POST /api/provider/profile
- GET  /api/provider/requests
- POST /api/provider/quotes

Admin:
- GET  /api/admin/requests
- GET  /api/admin/providers
- POST /api/admin/providers/{id}/approve
- POST /api/admin/providers/{id}/suspend
- GET  /api/admin/requests/{id}

LOCAL DEV WITH DOCKER (REQUIRED)
- Provide docker-compose.yml at repo root that starts:
  - SQL Server (mcr.microsoft.com/mssql/server) with a known SA password from env
- Backend must connect to SQL Server container via connection string env var.
- Provide scripts/commands in README:
  - docker compose up -d
  - apply migrations (or auto-apply in Development)
  - run backend
  - run frontend
- Include a health check or clear troubleshooting notes for SQL startup timing.

DELIVERABLES (MUST INCLUDE)
1) Fully working codebase for Phase 0 + Phase 1 only.
2) Root README.md with crystal-clear local instructions:
   - prerequisites (Node, .NET 8, Docker Desktop)
   - env vars + .env.example for frontend and backend
   - how to start SQL Server via Docker
   - how to run migrations
   - how to run backend + frontend
   - where to find dev verification emails (/backend/dev-emails/)
   - how to test Phase 0 and Phase 1 end-to-end
3) Ensure “git clone → docker compose up → run backend → run frontend” works.

NOW EXECUTE
- Read CharterCompare.Phased.md
- Implement Phase 0 + Phase 1 only
- Keep it minimal, clean, and locally runnable
- Document all assumptions in README
