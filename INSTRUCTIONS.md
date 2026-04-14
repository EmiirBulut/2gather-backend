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

## Phase 7 — Bug Fixes

Bu phase'i yeni özelliklerden önce tamamla. Her adımı bitirdikten sonra `dotnet build` çalıştır.

### Step 7.1 — Silme ve güncelleme düzeltmeleri

`DeleteOptionCommand` ve `UpdateOptionCommand` handler'larında şunları doğrula:
- `optionId` ile entity fetch ediliyor mu?
- Fetch sonrası `option.Item.ListId` üzerinden caller'ın list member'ı olduğu kontrol ediliyor mu?
- Eksikse `IOptionRepository`'ye `GetByIdWithItemAsync(Guid optionId)` metodu ekle, option'ı item navigation property'siyle birlikte getir.

`DeleteItemCommand` ve `UpdateItemCommand` için de aynı kontrolü uygula: handler `itemId` ile item'ı fetch ediyor mu, ardından caller'ın list member'ı olduğunu doğruluyor mu?

### Step 7.2 — Ondalıklı tutar precision

`ItemOptionConfiguration.cs`'de `Price` alanının `HasPrecision(18, 2)` olarak tanımlı olduğunu doğrula. Eksikse düzelt ve migration ekle:
```
dotnet ef migrations add FixPricePrecision --project Infrastructure --startup-project Api
```
`ItemOptionDto`'da `Price` alanının `decimal?` tipinde olduğunu kontrol et — `double` veya `float` kullanılmışsa `decimal` olarak düzelt.

### Step 7.3 — Options count düzeltmesi

`GetItemsByListQuery` handler'ında `optionsCount` hesaplamasını kontrol et. Aşağıdaki gibi `.Select()` projeksiyonu içinde subquery olarak yazılmış olmalı:

```csharp
OptionsCount = context.ItemOptions.Count(o => o.ItemId == item.Id)
```

`Include()` ile navigation property yüklenip ardından `.Count` çağrılıyorsa bunu `.Select()` içi subquery'ye çevir — `Include` gereksiz veri çeker ve option'lar yüklenmemişse sıfır döner.

### Step 7.4 — Satın alındı butonu düzeltmesi

`MarkItemPurchasedCommand` handler'ında şunları sırayla doğrula:
- Item `Status` alanı `ItemStatus.Purchased` olarak set ediliyor mu?
- `PurchasedAt` = `IDateTimeService.UtcNow` olarak set ediliyor mu?
- `SaveChangesAsync` çağrılıyor mu?
- Handler sonunda `INotificationService.ItemPurchasedAsync` çağrılıyor mu?
- `PATCH /api/items/{itemId}/status` route'u controller'da doğru tanımlı mı?

Eksik olan her adımı tamamla.

**Checkpoint**: Swagger üzerinden tüm bug fix senaryolarını test et. Silme, güncelleme, options count ve purchased işlemleri doğru çalışıyor olmalı.

---

## Phase 8 — Seçenek Alanı Genişletme (Marka, Model, Renk)

### Step 8.1 — Domain değişikliği

`Domain/Entities/ItemOption.cs`'e üç yeni opsiyonel alan ekle:

```csharp
public string? Brand { get; set; }
public string? Model { get; set; }
public string? Color { get; set; }
```

### Step 8.2 — EF Core konfigürasyonu

`ItemOptionConfiguration.cs`'de yeni alanlar için:
- `Brand`: `HasMaxLength(100)`, nullable
- `Model`: `HasMaxLength(100)`, nullable
- `Color`: `HasMaxLength(50)`, nullable

Migration:
```
dotnet ef migrations add AddBrandModelColorToItemOption --project Infrastructure --startup-project Api
```

### Step 8.3 — DTO ve Command güncellemeleri

`ItemOptionDto`'ya `Brand?`, `Model?`, `Color?` ekle.

`CreateOptionCommand` ve `UpdateOptionCommand`'a aynı alanları ekle.

`CreateOptionCommandValidator` ve `UpdateOptionCommandValidator`'a ekle:
- `Brand`: opsiyonel, max 100 karakter
- `Model`: opsiyonel, max 100 karakter
- `Color`: opsiyonel, max 50 karakter

`CreateOptionCommandHandler` ve `UpdateOptionCommandHandler`'da yeni alanların entity'ye map edildiğini doğrula.

**Checkpoint**: `POST /api/items/{itemId}/options` body'sine `brand`, `model`, `color` eklenebiliyor ve `GET /api/items/{itemId}/options` yanıtında bu alanlar dönüyor olmalı.

---

## Phase 9 — Rating Sistemi

### Step 9.1 — Domain entity

`Domain/Entities/OptionRating.cs` oluştur:

```csharp
public Guid Id { get; set; }
public Guid OptionId { get; set; }
public ItemOption Option { get; set; } = null!;
public Guid UserId { get; set; }
public User User { get; set; } = null!;
public int Score { get; set; }           // 1-5 arası
public DateTime CreatedAt { get; set; }
public DateTime? UpdatedAt { get; set; }
```

`ItemOption` entity'sine navigation ekle:
```csharp
public ICollection<OptionRating> Ratings { get; set; } = new List<OptionRating>();
```

### Step 9.2 — EF Core konfigürasyonu

`Infrastructure/Persistence/Configurations/OptionRatingConfiguration.cs` oluştur:
- Primary key: `Id`
- Unique index: `(OptionId, UserId)` — bir kullanıcı bir seçeneğe yalnızca bir rating verebilir
- `Score` check constraint: `Score >= 1 AND Score <= 5`
- Cascade delete: `ItemOption` silinince `OptionRating`'ler silinir

Migration:
```
dotnet ef migrations add AddOptionRating --project Infrastructure --startup-project Api
```

### Step 9.3 — Repository interface

`Application/Common/Interfaces/IOptionRatingRepository.cs`:
```csharp
Task<OptionRating?> GetByOptionAndUserAsync(Guid optionId, Guid userId, CancellationToken ct);
Task AddAsync(OptionRating rating, CancellationToken ct);
Task SaveChangesAsync(CancellationToken ct);
```

### Step 9.4 — Command ve Query

`Application/Features/Ratings/` klasörü oluştur:

**`RateOptionCommand`** — `OptionId`, `Score` (1-5):
- Caller list üyesi olmalı — Viewer dahil herkes rating verebilir
- Seçenek mevcut olmalı, yoksa `NotFoundException`
- Aynı kullanıcı aynı seçeneği daha önce ratinglemişse güncelle (upsert), yoksa yeni kayıt ekle
- SignalR: `OptionRatingUpdated` eventi broadcast et: `{ listId, optionId, averageRating, totalRatings, currentUserScore }`

**`GetOptionRatingsQuery`** — `OptionId`:
- Tüm üyeler okuyabilir
- Dönen DTO: `averageRating (decimal)`, `totalRatings (int)`, `currentUserScore (int?)` — giriş yapan kullanıcının skoru

### Step 9.5 — DTO güncellemesi

`ItemOptionDto`'ya ekle:
```csharp
public decimal? AverageRating { get; set; }
public int TotalRatings { get; set; }
public int? CurrentUserScore { get; set; }
```

`GetOptionsByItemQuery` handler'ını güncelle: her option için rating özetini tek `.Select()` projeksiyonu içinde hesapla, ayrı sorgu açma.

### Step 9.6 — Controller

`Api/Controllers/RatingsController.cs` oluştur:
- `POST /api/options/{optionId}/ratings` — RateOptionCommand
- `GET  /api/options/{optionId}/ratings` — GetOptionRatingsQuery

`INotificationService`'e `OptionRatingUpdatedAsync(Guid listId, Guid optionId, CancellationToken ct)` ekle ve `SignalRNotificationService`'de implement et.

**Checkpoint**: Bir seçeneğe rating ver. Farklı bir kullanıcıyla aynı seçeneğe farklı rating ver. `GET /api/options/{optionId}/ratings` doğru ortalama ve `currentUserScore` dönüyor olmalı. Aynı kullanıcı rating'ini güncelleyebiliyor olmalı.

---

## Phase 10 — Nihai Karar Sistemi

### Step 10.1 — Domain değişikliği

`Domain/Entities/ItemOption.cs`'e ekle:

```csharp
public bool IsFinal { get; set; }
public DateTime? FinalizedAt { get; set; }
public Guid? FinalizedBy { get; set; }
```

Migration:
```
dotnet ef migrations add AddFinalDecisionToItemOption --project Infrastructure --startup-project Api
```

### Step 10.2 — SetFinalOptionCommand

`Application/Features/Options/SetFinalOptionCommand.cs`:
- Sadece `Owner` çalıştırabilir, değilse `ForbiddenException`
- Hedef seçenek bu item'a ait olmalı, değilse `NotFoundException`
- Aynı item'ın başka bir seçeneğinde `IsFinal=true` varsa önce onu temizle: `IsFinal=false`, `FinalizedAt=null`, `FinalizedBy=null`
- Hedef seçeneği set et: `IsFinal=true`, `FinalizedAt=UtcNow`, `FinalizedBy=currentUserId`
- SignalR: `OptionFinalized` eventi broadcast et: `{ listId, itemId, finalOptionId }`

### Step 10.3 — RemoveFinalDecisionCommand

`Application/Features/Options/RemoveFinalDecisionCommand.cs`:
- Sadece `Owner` çalıştırabilir
- Hedef seçeneğin `IsFinal=true` olması gerekir, değilse `DomainException`
- `IsFinal=false`, `FinalizedAt=null`, `FinalizedBy=null` yap
- SignalR: `OptionFinalRemoved` eventi broadcast et: `{ listId, itemId, optionId }`

### Step 10.4 — Mevcut command'lara kural ekleme

`UpdateOptionCommand` handler'ında: seçenek `IsFinal=true` ve caller `Owner` değilse `ForbiddenException`.

`DeleteOptionCommand` handler'ında: seçenek `IsFinal=true` ise `DomainException` — "Nihai kararı verilmiş seçenek silinemez. Önce nihai kararı kaldırın."

### Step 10.5 — DTO ve Controller güncellemeleri

`ItemOptionDto`'ya ekle:
```csharp
public bool IsFinal { get; set; }
public DateTime? FinalizedAt { get; set; }
```

`OptionsController.cs`'e ekle:
- `PATCH  /api/options/{optionId}/finalize` — SetFinalOptionCommand
- `DELETE /api/options/{optionId}/finalize` — RemoveFinalDecisionCommand

`INotificationService`'e `OptionFinalizedAsync` ve `OptionFinalRemovedAsync` ekle, `SignalRNotificationService`'de implement et.

**Checkpoint**: Owner bir seçeneği nihai yapabilir. Aynı item'da başka seçenek nihai yapılınca önceki otomatik temizlenir. Editor bu endpoint'i çağırırsa 403 alır. Nihai seçeneği Editor güncellemeye çalışırsa 403 alır. Nihai seçenek silinemez.

---

## Phase 11 — Talip Olma Sistemi

### Step 11.1 — Domain entity

`Domain/Entities/OptionClaim.cs` oluştur:

```csharp
public Guid Id { get; set; }
public Guid OptionId { get; set; }
public ItemOption Option { get; set; } = null!;
public Guid UserId { get; set; }
public User User { get; set; } = null!;
public int Percentage { get; set; }           // 25 / 50 / 75 / 100
public ClaimStatus Status { get; set; }       // Pending / Approved / Rejected
public DateTime CreatedAt { get; set; }
public DateTime? ReviewedAt { get; set; }
public Guid? ReviewedBy { get; set; }
```

`Domain/Enums/ClaimStatus.cs`:
```csharp
public enum ClaimStatus { Pending, Approved, Rejected }
```

`ItemOption` entity'sine navigation ekle:
```csharp
public ICollection<OptionClaim> Claims { get; set; } = new List<OptionClaim>();
```

### Step 11.2 — EF Core konfigürasyonu

`Infrastructure/Persistence/Configurations/OptionClaimConfiguration.cs`:
- Primary key: `Id`
- `Percentage` check constraint: değer 25, 50, 75 veya 100 olmalı
- Index: `(OptionId, UserId)` — unique değil, sorgu performansı için
- Cascade delete: `ItemOption` silinince `OptionClaim`'ler silinir

Migration:
```
dotnet ef migrations add AddOptionClaim --project Infrastructure --startup-project Api
```

### Step 11.3 — Repository interface

`Application/Common/Interfaces/IOptionClaimRepository.cs`:
```csharp
Task<OptionClaim?> GetByIdAsync(Guid id, CancellationToken ct);
Task<List<OptionClaim>> GetByOptionIdAsync(Guid optionId, CancellationToken ct);
Task<int> GetApprovedPercentageTotalAsync(Guid optionId, CancellationToken ct);
Task AddAsync(OptionClaim claim, CancellationToken ct);
Task SaveChangesAsync(CancellationToken ct);
```

### Step 11.4 — CreateClaimCommand

`Application/Features/Claims/CreateClaimCommand.cs` — `OptionId`, `Percentage`:

İş kuralları sırasıyla:
1. Caller list üyesi olmalı; `Viewer` rolü `ForbiddenException` alır
2. Hedef seçenek `IsFinal=true` olmalı — değilse `DomainException`: "Yalnızca nihai kararı verilmiş seçeneklere talip olunabilir"
3. Item `Status=Purchased` ise `DomainException`: "Satın alınan ürüne yeni talip eklenemez"
4. `approvedTotal = GetApprovedPercentageTotalAsync(optionId)` hesapla
5. `approvedTotal + Percentage > 100` ise `DomainException`: "Talep edilen yüzde kapasiteyi aşıyor. Kalan: {100 - approvedTotal}%"
6. `OptionClaim` oluştur, `Status=Pending`
7. SignalR: `ClaimCreated` eventi broadcast et: `{ listId, optionId, claim: ClaimDto }`
8. Owner'a özel bildirim: `INotificationService.ClaimPendingNotificationAsync` çağır

### Step 11.5 — ReviewClaimCommand

`Application/Features/Claims/ReviewClaimCommand.cs` — `ClaimId`, `Decision` (Approved/Rejected):

İş kuralları:
1. Sadece `Owner` çalıştırabilir
2. Claim `Status=Pending` olmalı — değilse `DomainException`
3. `Decision=Approved` ise: `approvedTotal`'ı yeniden hesapla (race condition koruması), `approvedTotal + claim.Percentage > 100` ise `DomainException`
4. `Status`, `ReviewedAt`, `ReviewedBy` güncelle
5. SignalR: `ClaimApproved` veya `ClaimRejected` eventi broadcast et: `{ listId, optionId, claim: ClaimDto }`

### Step 11.6 — GetClaimsByOptionQuery

`Application/Features/Claims/GetClaimsByOptionQuery.cs` — `OptionId`:
- Tüm list üyeleri okuyabilir
- Dönen: `ClaimDto[]` — her claim için `id`, `userId`, `displayName`, `percentage`, `status`, `createdAt`

### Step 11.7 — MarkItemPurchasedCommand güncellemesi

Mevcut `MarkItemPurchasedCommand` handler'ında yetki kontrolünü aşağıdaki mantıkla güncelle:

```
1. Item'ın final seçeneğini bul (IsFinal=true olan option)
2. Final seçeneğin Approved claim'leri var mı kontrol et
3. Var ise: caller bu claim'lerden birinin UserId'si mi? Değilse ForbiddenException
4. Yok ise: caller Owner mı? Değilse ForbiddenException
5. Kontrolü geçtiyse item'ı Purchased olarak işaretle
```

### Step 11.8 — DTO ve Controller güncellemeleri

`ClaimDto.cs` oluştur:
```csharp
public Guid Id { get; set; }
public Guid UserId { get; set; }
public string DisplayName { get; set; } = string.Empty;
public int Percentage { get; set; }
public ClaimStatus Status { get; set; }
public DateTime CreatedAt { get; set; }
```

`ItemOptionDto`'ya ekle:
```csharp
public int ApprovedClaimsTotal { get; set; }
public int RemainingClaimPercentage { get; set; }
public List<ClaimDto> Claims { get; set; } = new();
```

`GetOptionsByItemQuery` handler'ını güncelle: her option için claim özetini tek `.Select()` içinde hesapla.

`Api/Controllers/ClaimsController.cs` oluştur:
- `POST  /api/options/{optionId}/claims` — CreateClaimCommand
- `GET   /api/options/{optionId}/claims` — GetClaimsByOptionQuery
- `PATCH /api/claims/{claimId}/review` — ReviewClaimCommand

`INotificationService`'e ekle ve `SignalRNotificationService`'de implement et:
- `ClaimCreatedAsync(Guid listId, Guid optionId, ClaimDto claim, CancellationToken ct)`
- `ClaimReviewedAsync(Guid listId, Guid optionId, ClaimDto claim, CancellationToken ct)`
- `ClaimPendingNotificationAsync(Guid listId, Guid ownerUserId, ClaimDto claim, CancellationToken ct)`

**Checkpoint**:
- Nihai kararı verilmemiş seçeneğe talip olmaya çalışınca 422 alınır
- Nihai seçeneğe Editor talip olabilir, Pending durumunda görünür
- Owner claim'i onaylar, `approvedTotal` güncellenir, kalan yüzde düşer
- Toplam %100 dolan seçeneğe yeni talip eklenince 422 alınır
- Onaylı talibi olan kullanıcı ürünü satın alındı işaretleyebilir; hiç talip yoksa Owner işaretler

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
