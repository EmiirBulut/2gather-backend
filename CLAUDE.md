# Claude Backend Instructions — 2gather

> Stack: .NET 9, C#, ASP.NET Core Web API, Entity Framework Core, PostgreSQL, SignalR, JWT Authentication
> Place this file at the root of the backend repository.

---

## 1. Project Overview

2gather is a collaborative home planning application. Multiple users can share a single list, manage product items with options, and track spending by room/category. Real-time collaboration is handled via SignalR.

---

## 2. Project Structure

```
src/
├── Api/
│   ├── Controllers/            # HTTP endpoints only — no business logic
│   ├── Hubs/                   # SignalR hubs (ListHub)
│   ├── Middleware/             # Global error handling, request logging
│   └── Program.cs              # App entry point, DI registration, pipeline config
│
├── Application/
│   ├── Features/
│   │   ├── Auth/               # Register, Login, RefreshToken, Invite accept
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   └── DTOs/
│   │   ├── Lists/              # Create list, get list, update list, delete list
│   │   ├── Members/            # Invite member, remove member, update member role, invite management
│   │   ├── Items/              # Add item, update item, mark as purchased, delete item
│   │   ├── Options/            # Add option, update option, select option, finalize option, delete option
│   │   ├── Categories/         # List categories, add custom category
│   │   ├── Ratings/            # Rate option, get option ratings
│   │   ├── Claims/             # Create claim, review claim, get claims
│   │   ├── Notifications/      # Notification count query
│   │   └── Reports/            # Summary, by-category, spending breakdown, item list
│   ├── Common/
│   │   ├── Behaviors/          # ValidationBehavior, LoggingBehavior (MediatR pipeline)
│   │   └── Interfaces/         # ICurrentUserService, IDateTimeService, INotificationService, IFileStorageService, IEmailService
│   └── Mappings/               # Manual mapping methods (no AutoMapper — see §6)
│
├── Domain/
│   ├── Entities/               # User, RefreshToken, List, ListMember, ListInvite, Category, Item, ItemOption, OptionRating, OptionClaim
│   ├── Enums/                  # MemberRole, ItemStatus, ClaimStatus
│   └── Exceptions/             # DomainException, NotFoundException, ForbiddenException
│
└── Infrastructure/
    ├── Persistence/
    │   ├── AppDbContext.cs
    │   ├── Configurations/     # IEntityTypeConfiguration<T> per entity
    │   └── Repositories/       # Repository implementations
    └── Services/               # CurrentUserService, TokenService, DateTimeService, SignalRNotificationService, FileStorageService, ResendEmailService
```

---

## 3. Architecture

- Follow **Clean Architecture** strictly.
- Dependency direction: `Api` → `Application` → `Domain`. `Infrastructure` implements interfaces defined in `Application`.
- **Domain layer** has zero external dependencies.
- **Application layer** depends only on Domain — never reference EF Core or SignalR from Application.
- **Controllers** are thin — receive HTTP input, call MediatR, return result. No business logic.
- Use **CQRS via MediatR** for all feature operations.
- SignalR notification calls go through `INotificationService` defined in Application, implemented in Infrastructure — never call the hub directly from Application.

---

## 4. Domain Entities

```
User
  id (Guid), email, passwordHash, displayName, createdAt

RefreshToken
  id (Guid), userId (FK → User), tokenHash, expiresAt, createdAt, isRevoked

List
  id (Guid), name, ownerId (FK → User), createdAt

ListMember
  id (Guid), listId (FK → List), userId (FK → User), role (Owner/Editor/Viewer), joinedAt

ListInvite
  id (Guid), listId (FK → List), invitedEmail, token (unique), role (MemberRole),
  expiresAt, acceptedAt (nullable), createdAt

Category
  id (Guid), listId (FK → List, nullable — null = system default), name, roomLabel, isSystem (bool)

Item
  id (Guid), listId (FK → List), categoryId (FK → Category),
  name (short display label e.g. "Koltuk"),
  status (Pending/Purchased), purchasedAt (nullable),
  imageUrl (nullable, max 2000), planningNote (nullable, max 1000),
  createdAt, updatedAt

ItemOption
  id (Guid), itemId (FK → Item), title,
  price (decimal, nullable), currency (nullable), link (nullable), notes (nullable),
  brand (nullable, max 100), model (nullable, max 100), color (nullable, max 50),
  isSelected (bool),
  isFinal (bool), finalizedAt (nullable), finalizedBy (Guid, nullable),
  createdAt

OptionRating
  id (Guid), optionId (FK → ItemOption), userId (FK → User),
  score (int, 1-5), createdAt, updatedAt (nullable)
  — unique index on (optionId, userId)

OptionClaim
  id (Guid), optionId (FK → ItemOption), userId (FK → User),
  percentage (int — 25/50/75/100), status (ClaimStatus),
  createdAt, reviewedAt (nullable), reviewedBy (Guid, nullable)
```

**Key design decisions:**
- `Item.name` is the short label shown on the main list (e.g. "Koltuk"), not the full product name.
- `ItemOption` holds the actual product details. Multiple options per item.
- `ItemOption.brand/model/color` are the "technical details" shown in the item detail right panel.
- `Item.planningNote` is shown in the dark green planning note card on the item detail right panel.
- `Item.imageUrl` is a URL to the product image shown on item cards and item detail page.
- `Item.updatedAt` is updated on every write — used for "Son Güncelleme" display on the frontend.
- When an `Item` is marked purchased, `status` → `Purchased`, disappears from default list view.
- `OptionRating`: one rating per user per option — upsert on subsequent ratings. Score 1-5.
- `OptionClaim`: only allowed on `IsFinal=true` options. Percentage values: 25/50/75/100 only.
- `ClaimStatus`: Pending → Owner reviews → Approved or Rejected.
- `Category` can be system-defined (null `listId`) or list-specific custom categories.
- Soft-delete is **not** used anywhere — hard delete only.

---

## 5. Naming Conventions

| Element | Convention | Example |
|--------|-----------|---------|
| Classes | PascalCase | `ItemService`, `GetItemsByListQuery` |
| Interfaces | `I` prefix | `IItemRepository` |
| Methods | PascalCase | `GetItemsByListIdAsync` |
| Parameters / locals | camelCase | `listId`, `cancellationToken` |
| Private fields | `_` prefix | `_dbContext`, `_logger` |
| DTOs | `Dto` suffix | `ItemDto`, `CreateItemOptionRequestDto` |
| Commands | `Command` suffix | `MarkItemPurchasedCommand` |
| Queries | `Query` suffix | `GetListSummaryQuery` |
| Handlers | `Handler` suffix | `MarkItemPurchasedCommandHandler` |
| Enums | PascalCase values | `MemberRole.Editor`, `ItemStatus.Purchased`, `ClaimStatus.Approved` |

---

## 6. Mapping

- Use **manual mapping methods** — no AutoMapper.
- Define `static` mapping extension methods or private methods in the handler:

```csharp
private static ItemDto MapToDto(Item item) => new(
    Id: item.Id,
    Name: item.Name,
    Status: item.Status,
    CategoryId: item.CategoryId,
    ImageUrl: item.ImageUrl,
    PlanningNote: item.PlanningNote,
    UpdatedAt: item.UpdatedAt,
    OptionsCount: item.Options.Count
);
```

- Keep mapping close to where it's used (in the handler or a dedicated `Mappers/` static class per feature).

---

## 7. Entity Framework Core

- One `AppDbContext`. Use `IEntityTypeConfiguration<T>` for all entity config — no data annotations on entities.
- Register via `modelBuilder.ApplyConfigurationsFromAssembly(...)`.
- Migration naming: descriptive → `AddUserTable`, `AddOptionRatingTable`, `AddFinalDecisionToItemOption`.
- Use `AsNoTracking()` for all read-only queries.
- Project with `.Select()` — never load full entity graphs when only a subset of fields is needed.
- Never expose `IQueryable` outside the repository layer.
- Key configuration constraints:
  - `ItemOption.Price`: `HasPrecision(18, 2)`
  - `OptionRating.Score`: check constraint `Score >= 1 AND Score <= 5`
  - `OptionRating`: unique index on `(OptionId, UserId)`
  - `OptionClaim.Percentage`: check constraint — value must be 25, 50, 75, or 100
  - `ListMember`: unique index on `(ListId, UserId)`
  - `User.Email`: unique index
  - `ListInvite.Token`: unique index
  - Cascade deletes: List → ListMember, Item; Item → ItemOption; ItemOption → OptionRating, OptionClaim

---

## 8. API Design

- Controllers: HTTP concerns only. `[ApiController]`, `[Route("api/[controller]")]` on all.
- Return `ActionResult<T>` with correct HTTP status codes.
- Use DTOs for all input/output — never expose domain entities through the API.
- Validate all requests via **FluentValidation** in the MediatR pipeline behavior.
- Global error handling via middleware — no try/catch in controllers.

### Core Endpoints (reference)

```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/invite/accept

GET    /api/lists
POST   /api/lists
GET    /api/lists/{listId}
DELETE /api/lists/{listId}
GET    /api/lists/{listId}/notifications/count

POST   /api/lists/{listId}/members/invite
DELETE /api/lists/{listId}/members/{userId}
PATCH  /api/lists/{listId}/members/{userId}/role
GET    /api/lists/{listId}/members/invites
DELETE /api/lists/{listId}/members/invites/{inviteId}
POST   /api/lists/{listId}/members/invites/{inviteId}/resend

GET    /api/lists/{listId}/items?status=pending|purchased|all
POST   /api/lists/{listId}/items
GET    /api/items/{itemId}
PUT    /api/items/{itemId}
PATCH  /api/items/{itemId}/status
POST   /api/items/{itemId}/image
DELETE /api/items/{itemId}

GET    /api/items/{itemId}/options
POST   /api/items/{itemId}/options
PUT    /api/options/{optionId}
DELETE /api/options/{optionId}
PATCH  /api/options/{optionId}/select
PATCH  /api/options/{optionId}/finalize
DELETE /api/options/{optionId}/finalize

POST   /api/options/{optionId}/ratings
GET    /api/options/{optionId}/ratings

POST   /api/options/{optionId}/claims
GET    /api/options/{optionId}/claims
PATCH  /api/claims/{claimId}/review

GET    /api/categories?listId={listId}
POST   /api/lists/{listId}/categories

GET    /api/lists/{listId}/reports/summary
GET    /api/lists/{listId}/reports/by-category
GET    /api/lists/{listId}/reports/spending
GET    /api/lists/{listId}/reports/items
```

### Error Response Format

```json
{
  "status": 400,
  "error": "Validation failed",
  "details": ["Name is required", "Price must be a positive number"]
}
```

---

## 9. Authorization & Role Enforcement

- JWT Bearer with refresh token strategy.
- Access token: 15 minutes. Refresh token: 7 days, stored hashed in DB.
- Three roles per list: `Owner`, `Editor`, `Viewer`.
- **Owner**: full control — invite/remove members, delete list, set final option, review claims, manage invites.
- **Editor**: create/edit/delete items and options, rate options, create claims, mark purchased (if approved claimant).
- **Viewer**: read-only. Can rate options. Cannot create claims or modify anything.
- Role checks happen in **command/query handlers** via `ICurrentUserService` — not in controllers.
- Invite system: GUID token stored in DB, sent via email, user calls `/api/auth/invite/accept`.

### Specific authorization rules

| Action | Allowed roles |
|---|---|
| Set final option (`PATCH /finalize`) | Owner only |
| Remove final decision (`DELETE /finalize`) | Owner only |
| Update a finalized option | Owner only |
| Delete a finalized option | Nobody — must remove finalization first |
| Create claim | Editor, Owner (Viewer cannot) |
| Review claim (approve/reject) | Owner only |
| Mark item purchased | Approved claimants OR Owner (if no approved claimants) |
| Manage invites (cancel/resend) | Owner only |
| View notification count | Owner only |
| Rate an option | All roles including Viewer |

---

## 10. SignalR — Real-time Collaboration

- Hub: `ListHub` at `/hubs/list`.
- Clients join a group per list: `await Groups.AddToGroupAsync(connectionId, $"list-{listId}")`.
- After any mutation, the handler calls `INotificationService` which broadcasts to the list group.
- Do not call SignalR from within a database transaction — notify after the transaction commits.
- SignalR authentication: pass JWT via query string (`?access_token=...`) for WebSocket connections.

### SignalR events

```
ItemAdded               { listId, item: ItemDto }
ItemUpdated             { listId, item: ItemDto }
ItemPurchased           { listId, itemId, purchasedAt }
ItemDeleted             { listId, itemId }
ItemImageUpdated        { listId, itemId, imageUrl }

OptionAdded             { listId, itemId, option: ItemOptionDto }
OptionUpdated           { listId, itemId, option: ItemOptionDto }
OptionDeleted           { listId, itemId, optionId }
OptionFinalized         { listId, itemId, finalOptionId }
OptionFinalRemoved      { listId, itemId, optionId }
OptionRatingUpdated     { listId, optionId }

ClaimCreated            { listId, optionId, claim: ClaimDto }
ClaimReviewed           { listId, optionId, claim: ClaimDto }
NotificationCountChanged { listId, pendingClaimsCount, pendingInvitesCount, totalNew }

MemberJoined            { listId, member: MemberDto }
MemberRemoved           { listId, userId }
```

**`NotificationCountChanged`** is sent only to the Owner — not the entire list group:
```csharp
await Clients.User(ownerUserId.ToString()).SendAsync("NotificationCountChanged", payload);
```

---

## 11. Dependency Injection

- Register in `Program.cs` or via extension methods (`services.AddApplicationServices()`, `services.AddInfrastructureServices()`).
- `AddScoped` for repositories, DbContext, SignalR-related services.
- `AddSingleton` for stateless thread-safe services.
- `AddTransient` for lightweight stateless utilities.
- Never use service locator pattern.

---

## 12. Async / Await

- All I/O operations must be async.
- Always pass and propagate `CancellationToken` from controller → handler → repository.
- Never use `.Result` or `.Wait()`. Never use `async void`.

---

## 13. Logging

- Use built-in `ILogger<T>`.
- `Information`: significant events (user registered, item marked purchased, member invited, claim approved).
- `Warning`: unexpected but recoverable (invite token expired, duplicate request, claim over capacity).
- `Error`: failures requiring attention (DB failure, unhandled exception).
- `Debug`: development only — do not leave in production code.
- Never log passwords, tokens, or PII.

---

## 14. Configuration

- All configurable values in `appsettings.json`. Never hardcode connection strings or secrets.
- Use `IOptions<T>` pattern for typed config binding.
- Secrets in environment variables or `appsettings.Development.json` (git-ignored).
- Required config sections: `ConnectionStrings:DefaultConnection`, `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience`, `Resend:ApiKey`, `Resend:FromEmail`, `Resend:FromName`, `Resend:AppBaseUrl`, `Invite:TokenExpiryHours`.

---

## 15. Code Quality

- Every public method performing I/O must be async and return `Task` or `Task<T>`.
- No magic strings or numbers — use constants or enums.
- No empty catch blocks — always log or re-throw.
- No committed TODOs without an explicit issue reference.
- Keep methods short and focused. If a method exceeds ~30 lines, consider splitting.
- No AutoMapper — manual mapping only (see §6).

---

*Last updated: 2026-04-22*
