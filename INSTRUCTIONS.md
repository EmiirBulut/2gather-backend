# 2gather — Backend Development Instructions
# For use with Claude Code (claude-cli)
# Place at: backend-repo/INSTRUCTIONS.md

---

## Project Context

You are building the backend for "2gather" — a collaborative wedding planning tool.
Read CLAUDE.md in this repository before writing any code. All architectural decisions, naming
conventions, and patterns are defined there. Follow them strictly.

---

## How to Work

- Work in small, focused steps. Complete one feature fully before starting the next.
- After generating code, verify it compiles (`dotnet build`) before declaring it done.
- Run existing tests after each change (`dotnet test`). Do not break passing tests.
- When adding a new feature, create the files in this order:
  1. Domain entity (if new)
  2. EF Core configuration
  3. Migration
  4. Repository interface (Application layer)
  5. Repository implementation (Infrastructure layer)
  6. Command or Query + DTO
  7. Handler
  8. Controller endpoint
  9. FluentValidation validator
- Never skip steps. Never put business logic in a controller.

---

## Phase 1 — Project Scaffold & Auth

### Step 1.1 — Solution and project setup
Create a .NET 9 solution with four projects:
- `TwoGather.Api` (ASP.NET Core Web API)
- `TwoGather.Application` (Class Library)
- `TwoGather.Domain` (Class Library)
- `TwoGather.Infrastructure` (Class Library)

Add project references following Clean Architecture direction:
`Api` → `Application` → `Domain`; `Infrastructure` → `Application`.

Install packages:
- Api: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Swashbuckle.AspNetCore`, `Microsoft.AspNetCore.SignalR`
- Application: `MediatR`, `FluentValidation`, `FluentValidation.DependencyInjectionExtensions`
- Infrastructure: `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `BCrypt.Net-Next`

### Step 1.2 — Domain entities
Create in `Domain/Entities/`:
- `User.cs` — Id (Guid), Email (string), PasswordHash (string), DisplayName (string), CreatedAt (DateTime)
- `List.cs` — Id (Guid), Name (string), OwnerId (Guid), Owner (User nav), Members (ICollection<ListMember>), Items (ICollection<Item>), CreatedAt (DateTime)
- `ListMember.cs` — Id (Guid), ListId (Guid), UserId (Guid), Role (MemberRole enum), JoinedAt (DateTime)
- `Category.cs` — Id (Guid), ListId (Guid, nullable), Name (string), RoomLabel (string), IsSystem (bool)
- `Item.cs` — Id (Guid), ListId (Guid), CategoryId (Guid), Name (string), Status (ItemStatus enum), PurchasedAt (DateTime, nullable), CreatedAt (DateTime)
- `ItemOption.cs` — Id (Guid), ItemId (Guid), Title (string), Price (decimal, nullable), Currency (string, nullable), Link (string, nullable), Notes (string, nullable), IsSelected (bool), CreatedAt (DateTime)

Create in `Domain/Enums/`:
- `MemberRole.cs` — Owner, Editor, Viewer
- `ItemStatus.cs` — Pending, Purchased

Create in `Domain/Exceptions/`:
- `NotFoundException.cs` — inherits Exception, takes (string entityName, object key)
- `ForbiddenException.cs` — inherits Exception
- `DomainException.cs` — inherits Exception

### Step 1.3 — EF Core setup
Create `Infrastructure/Persistence/AppDbContext.cs`.
Create `IEntityTypeConfiguration<T>` files in `Infrastructure/Persistence/Configurations/` for every entity.
Key configuration rules:
- All primary keys are Guid, database-generated.
- Email is unique index on User.
- ListMember has unique index on (ListId, UserId).
- ItemOption.Price has precision (18, 2).
- Cascade delete: deleting a List cascades to ListMember, Item. Deleting an Item cascades to ItemOption.

Run: `dotnet ef migrations add InitialCreate --project Infrastructure --startup-project Api`

### Step 1.4 — Interfaces and services
Create in `Application/Common/Interfaces/`:
- `ICurrentUserService.cs` — `Guid UserId { get; }`, `string Email { get; }`
- `IDateTimeService.cs` — `DateTime UtcNow { get; }`
- `ITokenService.cs` — `string GenerateAccessToken(User user)`, `string GenerateRefreshToken()`, `ClaimsPrincipal? ValidateToken(string token)`
- `INotificationService.cs` — methods matching each SignalR event (see CLAUDE.md §10)
- `IListRepository.cs`, `IItemRepository.cs`, `IOptionRepository.cs`, `ICategoryRepository.cs`, `IUserRepository.cs`

Implement all interfaces in `Infrastructure/Services/` and `Infrastructure/Persistence/Repositories/`.
Implement `INotificationService` in `Infrastructure/Services/SignalRNotificationService.cs` using `IHubContext<ListHub>`.

### Step 1.5 — Auth feature
Create `Application/Features/Auth/` with:
- `RegisterCommand` + `RegisterCommandHandler` + `RegisterRequestDto` / `AuthResponseDto`
- `LoginCommand` + `LoginCommandHandler` + `LoginRequestDto`
- `RefreshTokenCommand` + `RefreshTokenCommandHandler`

Store refresh tokens hashed in a `RefreshToken` table (add entity + migration):
- Id (Guid), UserId (Guid), TokenHash (string), ExpiresAt (DateTime), CreatedAt (DateTime), IsRevoked (bool)

Create `Api/Controllers/AuthController.cs` with:
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`

### Step 1.6 — MediatR pipeline behaviors
Create `Application/Common/Behaviors/`:
- `ValidationBehavior.cs` — runs FluentValidation before handler, throws `ValidationException` on failure
- `LoggingBehavior.cs` — logs handler name and elapsed time at Information level

Register both in `Program.cs`.

### Step 1.7 — Global error middleware
Create `Api/Middleware/ExceptionHandlingMiddleware.cs`.
Map exceptions to HTTP responses:
- `NotFoundException` → 404
- `ForbiddenException` → 403
- `ValidationException` (FluentValidation) → 400 with details array
- `DomainException` → 422
- Unhandled → 500

Response format must match CLAUDE.md §8 exactly.

### Step 1.8 — Swagger + CORS + DI wiring
Configure Swagger with JWT bearer auth support.
Configure CORS to allow the frontend dev origin (`http://localhost:5173`).
Register all services, repositories, MediatR, and FluentValidation validators in `Program.cs`.

**Checkpoint**: `dotnet build` passes. `POST /api/auth/register` and `POST /api/auth/login` return tokens. Swagger UI is accessible at `/swagger`.

---

## Phase 2 — Lists and Members

### Step 2.1 — List CRUD
Commands/Queries in `Application/Features/Lists/`:
- `CreateListCommand` — creates list, automatically adds creator as Owner member
- `GetUserListsQuery` — returns all lists the current user is a member of
- `GetListByIdQuery` — returns list detail; throws `ForbiddenException` if caller is not a member
- `DeleteListCommand` — only Owner can delete

Controller: `Api/Controllers/ListsController.cs`
- `GET    /api/lists`
- `POST   /api/lists`
- `GET    /api/lists/{listId}`
- `DELETE /api/lists/{listId}`

### Step 2.2 — Invite system
Add to `Domain/Entities/`: `ListInvite.cs`
- Id (Guid), ListId (Guid), InvitedEmail (string), Token (string, unique), Role (MemberRole), ExpiresAt (DateTime), AcceptedAt (DateTime, nullable), CreatedAt (DateTime)

Commands in `Application/Features/Members/`:
- `InviteMemberCommand` — Owner/Editor only; generates GUID token; sends email via `IEmailService` (stub the implementation for now — log to console)
- `AcceptInviteCommand` — validates token, creates `ListMember`, marks invite accepted
- `RemoveMemberCommand` — Owner only; cannot remove self
- `UpdateMemberRoleCommand` — Owner only

Controller: `Api/Controllers/MembersController.cs`
- `POST   /api/lists/{listId}/members/invite`
- `DELETE /api/lists/{listId}/members/{userId}`
- `PATCH  /api/lists/{listId}/members/{userId}/role`
- `POST   /api/auth/invite/accept` (in AuthController)

### Step 2.3 — Helper: role enforcement
Create a shared internal helper `ListAuthorizationHelper` used by handlers:
```csharp
void RequireRole(ListMember? member, params MemberRole[] allowed);
// throws ForbiddenException if member is null or role not in allowed
```

**Checkpoint**: A user can create a list, invite another user by email, the second user accepts the invite and can see the list.

---

## Phase 3 — Categories, Items, Options

### Step 3.1 — System categories seed
On application startup (or via a migration), seed the system categories:
Salon, Yatak Odası, Mutfak, Banyo, Çocuk Odası, Genel

### Step 3.2 — Categories
Commands/Queries in `Application/Features/Categories/`:
- `GetCategoriesQuery` — returns system categories + list-specific custom categories for a given listId
- `CreateCustomCategoryCommand` — Editor/Owner only; creates a category scoped to the list

Controller: `Api/Controllers/CategoriesController.cs`
- `GET  /api/categories?listId={listId}`
- `POST /api/lists/{listId}/categories`

### Step 3.3 — Items
Commands/Queries in `Application/Features/Items/`:
- `GetItemsByListQuery` — accepts `?status=pending|purchased|all`, returns items with option count and selected option summary; Editor/Owner/Viewer can read
- `CreateItemCommand` — Editor/Owner only
- `UpdateItemCommand` — Editor/Owner only (rename, change category)
- `MarkItemPurchasedCommand` — Editor/Owner only; sets Status=Purchased, PurchasedAt=UtcNow; calls `INotificationService.ItemPurchasedAsync`
- `DeleteItemCommand` — Editor/Owner only

After each mutation, call the appropriate `INotificationService` method.

Controller: `Api/Controllers/ItemsController.cs`
- `GET    /api/lists/{listId}/items`
- `POST   /api/lists/{listId}/items`
- `PATCH  /api/items/{itemId}/status`
- `PUT    /api/items/{itemId}`
- `DELETE /api/items/{itemId}`

### Step 3.4 — Item Options
Commands/Queries in `Application/Features/Options/`:
- `GetOptionsByItemQuery` — Editor/Owner/Viewer can read
- `CreateOptionCommand` — Editor/Owner only
- `UpdateOptionCommand` — Editor/Owner only (price, link, notes, title)
- `SelectOptionCommand` — Editor/Owner only; sets IsSelected=true on this option, IsSelected=false on all others for the same item
- `DeleteOptionCommand` — Editor/Owner only

After each mutation, call the appropriate `INotificationService` method.

Controller: `Api/Controllers/OptionsController.cs`
- `GET    /api/items/{itemId}/options`
- `POST   /api/items/{itemId}/options`
- `PUT    /api/options/{optionId}`
- `PATCH  /api/options/{optionId}/select`
- `DELETE /api/options/{optionId}`

**Checkpoint**: Full CRUD on items and options works. Postman/Swagger tests pass for all endpoints with proper role enforcement.

---

## Phase 4 — SignalR Hub

### Step 4.1 — ListHub
Create `Api/Hubs/ListHub.cs`:
- Authenticate via JWT (token from query string `access_token`).
- `JoinList(string listId)` — adds connection to group `list-{listId}` after verifying caller is a member.
- `LeaveList(string listId)` — removes from group.

### Step 4.2 — Implement INotificationService
Complete `Infrastructure/Services/SignalRNotificationService.cs`:
- Inject `IHubContext<ListHub>`.
- For each event method, call `Clients.Group($"list-{listId}").SendAsync(eventName, payload)`.
- All methods must be async and accept CancellationToken.

### Step 4.3 — Wire SignalR in Program.cs
```csharp
builder.Services.AddSignalR();
app.MapHub<ListHub>("/hubs/list");
```
Configure JWT to read from query string for SignalR connections.

**Checkpoint**: Open two browser tabs, both connected to SignalR. Mark an item purchased in one tab — the other tab receives the `ItemPurchased` event.

---

## Phase 5 — Reports

### Step 5.1 — Report queries
Create `Application/Features/Reports/`:
- `GetListSummaryQuery` — returns: totalItems, pendingCount, purchasedCount, totalSpent (sum of selected option prices), estimatedTotal (sum of all selected option prices including pending)
- `GetCategoryBreportQuery` — per category: name, totalItems, pendingCount, purchasedCount, spent
- `GetSpendingBreakdownQuery` — purchased items with their selected option price, grouped by category

All report queries: any member (Viewer included) can access. Use `.AsNoTracking()` and `.Select()` projections only.

Controller: `Api/Controllers/ReportsController.cs`
- `GET /api/lists/{listId}/reports/summary`
- `GET /api/lists/{listId}/reports/by-category`
- `GET /api/lists/{listId}/reports/spending`

**Checkpoint**: All report endpoints return correct aggregated data.

---

## Phase 6 — Tests

### Step 6.1 — Unit tests
Create `tests/Application.Tests/` project.
Write unit tests for:
- `ValidationBehavior` — ensure invalid commands return validation errors
- `MarkItemPurchasedCommandHandler` — assert status changes, notification called, forbidden for Viewer
- `InviteMemberCommandHandler` — assert invite created, email service called
- `AcceptInviteCommandHandler` — assert member added, expired token rejected

Use `xUnit` + `Moq` (or `NSubstitute`).

### Step 6.2 — Integration tests (optional but recommended)
Create `tests/Api.IntegrationTests/` project.
Use `WebApplicationFactory<Program>` with an in-memory or test PostgreSQL database.
Test the full HTTP stack for auth register/login + create list + add item.

---

## General Rules for Every Code Change

1. Follow CLAUDE.md conventions without exception.
2. Never put business logic in a Controller.
3. Never reference EF Core from Application layer.
4. Always pass CancellationToken from Controller to Handler to Repository.
5. Every handler must check that the current user is a member of the list before acting.
6. After every mutation, call INotificationService — never forget this.
7. Run `dotnet build` after every file creation. Fix errors before continuing.
8. Do not use AutoMapper — use manual mapping as shown in CLAUDE.md §6.
9. Do not add packages not listed here without asking first.
10. Commit-ready code only: no TODOs, no dead code, no console logs.
