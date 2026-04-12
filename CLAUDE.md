# Claude Backend Instructions — 2gather App

> Stack: .NET 9, C#, ASP.NET Core Web API, Entity Framework Core, PostgreSQL, SignalR, JWT Authentication
> Place this file at the root of the backend repository.

---

## 1. Project Overview

A collaborative wedding planning application (2gather). Multiple users can share a single list, manage product items with options, and track spending by room/category. Real-time collaboration is handled via SignalR.

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
│   │   ├── Members/            # Invite member, remove member, update member role
│   │   ├── Items/              # Add item, update item, mark as purchased, delete item
│   │   ├── Options/            # Add option, update option, select option, delete option
│   │   ├── Categories/         # List categories, add custom category
│   │   └── Reports/            # Summary, by-category, spending breakdown
│   ├── Common/
│   │   ├── Behaviors/          # ValidationBehavior, LoggingBehavior (MediatR pipeline)
│   │   └── Interfaces/         # ICurrentUserService, IDateTimeService, INotificationService
│   └── Mappings/               # Manual mapping methods (no AutoMapper — see §6)
│
├── Domain/
│   ├── Entities/               # User, List, ListMember, Category, Item, ItemOption
│   ├── Enums/                  # MemberRole, ItemStatus, RoomCategory
│   └── Exceptions/             # DomainException, NotFoundException, ForbiddenException
│
└── Infrastructure/
    ├── Persistence/
    │   ├── AppDbContext.cs
    │   ├── Configurations/     # IEntityTypeConfiguration<T> per entity
    │   └── Repositories/       # Repository implementations
    └── Services/               # CurrentUserService, TokenService, InviteService, DateTimeService
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

List
  id (Guid), name, ownerId (FK → User), createdAt

ListMember
  id (Guid), listId (FK → List), userId (FK → User), role (Owner/Editor/Viewer), joinedAt

Category
  id (Guid), listId (FK → List, nullable — null = system default), name, roomLabel, isSystem (bool)

Item
  id (Guid), listId (FK → List), categoryId (FK → Category), name (display name shown on list),
  status (Pending/Purchased), purchasedAt (nullable), createdAt

ItemOption
  id (Guid), itemId (FK → Item), title, price (decimal, nullable), currency (string, nullable),
  link (string, nullable), notes (string, nullable), isSelected (bool), createdAt
```

**Key design decisions:**
- `Item.name` is the short label shown on the main list (e.g. "Koltuk"), not the full product name.
- `ItemOption` holds the actual product details (price, link, notes). Multiple options per item.
- When an `Item` is marked purchased, `status` changes to `Purchased` and it disappears from the default list view (filtered out). It remains accessible via the "purchased" filter.
- `Category` can be system-defined (null `listId`) or list-specific custom categories.

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
| Enums | PascalCase values | `MemberRole.Editor`, `ItemStatus.Purchased` |

---

## 6. Mapping

- Use **manual mapping methods** — no AutoMapper.
- Define `static` mapping extension methods or private methods in the handler, e.g.:

```csharp
private static ItemDto MapToDto(Item item) => new(
    Id: item.Id,
    Name: item.Name,
    Status: item.Status,
    CategoryId: item.CategoryId,
    OptionsCount: item.Options.Count
);
```

- Keep mapping close to where it's used (in the handler or a dedicated `Mappers/` static class per feature).

---

## 7. Entity Framework Core

- One `AppDbContext`. Use `IEntityTypeConfiguration<T>` for all entity config — no data annotations on entities.
- Register via `modelBuilder.ApplyConfigurationsFromAssembly(...)`.
- Migration naming: descriptive → `AddUserTable`, `AddListMemberTable`, `AddItemOptionTable`.
- Use `AsNoTracking()` for all read-only queries.
- Project with `.Select()` — never load full entity graphs when only a subset of fields is needed.
- Never expose `IQueryable` outside the repository layer.
- Soft-delete is **not** used — hard delete only. Purchased items are filtered by status, not deleted.

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

POST   /api/lists/{listId}/members/invite
DELETE /api/lists/{listId}/members/{userId}
PATCH  /api/lists/{listId}/members/{userId}/role

GET    /api/lists/{listId}/items?status=pending|purchased
POST   /api/lists/{listId}/items
PATCH  /api/items/{itemId}/status
DELETE /api/items/{itemId}

GET    /api/items/{itemId}/options
POST   /api/items/{itemId}/options
PUT    /api/options/{optionId}
DELETE /api/options/{optionId}
PATCH  /api/options/{optionId}/select

GET    /api/categories?listId={listId}
POST   /api/lists/{listId}/categories

GET    /api/lists/{listId}/reports/summary
GET    /api/lists/{listId}/reports/by-category
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
- **Owner**: full control (invite, remove members, delete list, edit everything).
- **Editor**: create/edit/delete items and options, mark purchased.
- **Viewer**: read-only access.
- Role checks happen in **command/query handlers** via `ICurrentUserService` — not in controllers.
- Invite system: generate a short-lived signed token (JWT or GUID stored in DB), send via email, user calls `/api/auth/invite/accept` with the token.

---

## 10. SignalR — Real-time Collaboration

- Hub: `ListHub` at `/hubs/list`.
- Clients join a group per list: `await Groups.AddToGroupAsync(connectionId, $"list-{listId}")`.
- After any mutation (item added, status changed, option updated), the handler calls `INotificationService` which broadcasts to the list group.
- SignalR events to broadcast:

```
ItemAdded       { listId, item: ItemDto }
ItemUpdated     { listId, item: ItemDto }
ItemPurchased   { listId, itemId, purchasedAt }
ItemDeleted     { listId, itemId }
OptionAdded     { listId, itemId, option: ItemOptionDto }
OptionUpdated   { listId, itemId, option: ItemOptionDto }
OptionDeleted   { listId, itemId, optionId }
MemberJoined    { listId, member: MemberDto }
MemberRemoved   { listId, userId }
```

- Do not call SignalR from within a database transaction — notify after the transaction commits.
- SignalR authentication: pass JWT via query string (`?access_token=...`) for WebSocket connections.

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
- `Information`: significant events (user registered, item marked purchased, member invited).
- `Warning`: unexpected but recoverable (invite token expired, duplicate request).
- `Error`: failures requiring attention (DB failure, unhandled exception).
- `Debug`: development only — do not leave in production code.
- Never log passwords, tokens, or PII.

---

## 14. Configuration

- All configurable values in `appsettings.json`. Never hardcode connection strings or secrets.
- Use `IOptions<T>` pattern for typed config binding.
- Secrets in environment variables or `appsettings.Development.json` (git-ignored).
- Required config sections: `ConnectionStrings:DefaultConnection`, `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience`, `Email:SmtpHost`, `Invite:TokenExpiryHours`.

---

## 15. Code Quality

- Every public method performing I/O must be async and return `Task` or `Task<T>`.
- No magic strings or numbers — use constants or enums.
- No empty catch blocks — always log or re-throw.
- No committed TODOs without an explicit issue reference.
- Keep methods short and focused. If a method exceeds ~30 lines, consider splitting.
- No AutoMapper — manual mapping only (see §6).

---

*Last updated: 2026-04-12*
