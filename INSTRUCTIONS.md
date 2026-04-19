# 2gather — Backend UI Redesign Instructions
# For use with Claude Code (claude-cli)
# Place at: backend-repo/INSTRUCTIONS_UI.md
# Bu dosya mevcut INSTRUCTIONS.md'ye (Phase 1-11) ek olarak uygulanır.
# Önce mevcut INSTRUCTIONS.md'yi, ardından bu dosyayı oku.
# Bu phase'leri INSTRUCTIONS.md'deki Phase 11 tamamlandıktan sonra uygula.

---

## Genel Bakış

Frontend UI redesign'ında ortaya çıkan yeni veri ihtiyaçları:

1. `Item` entity'sine görsel (`imageUrl`) ve planlama notu (`planningNote`) alanları eklenmeli
2. `GetUserListsQuery` dashboard için ek istatistik döndürmeli (üye sayısı, item ilerlemesi, tamamlanma yüzdesi)
3. `GetListByIdQuery` proje dashboard için kategori bazlı özet ve bekleyen talepler döndürmeli
4. Davet yönetimi: bekleyen davetleri listeleme ve iptal etme endpoint'leri eksik
5. `GetItemsByListQuery` görsel URL'ini döndürmeli

---

## Phase 12 — Item Görsel ve Planlama Notu

### Step 12.1 — Domain değişikliği

`Domain/Entities/Item.cs`'e ekle:

```csharp
public string? ImageUrl { get; set; }
public string? PlanningNote { get; set; }
```

### Step 12.2 — EF Core konfigürasyonu

`ItemConfiguration.cs`'de yeni alanlar için:
- `ImageUrl`: `HasMaxLength(2000)`, nullable
- `PlanningNote`: `HasMaxLength(1000)`, nullable

Migration:
```
dotnet ef migrations add AddImageUrlAndPlanningNoteToItem --project Infrastructure --startup-project Api
```

### Step 12.3 — DTO güncellemeleri

`ItemDto`'ya ekle:
```csharp
public string? ImageUrl { get; set; }
public string? PlanningNote { get; set; }
```

`CreateItemCommand`'a ekle:
```csharp
public string? ImageUrl { get; set; }
public string? PlanningNote { get; set; }
```

`UpdateItemCommand`'a ekle:
```csharp
public string? ImageUrl { get; set; }
public string? PlanningNote { get; set; }
```

`CreateItemCommandValidator`'a ekle:
- `ImageUrl`: opsiyonel, geçerli URL formatı, max 2000 karakter
- `PlanningNote`: opsiyonel, max 1000 karakter

`UpdateItemCommandValidator`'a aynı kuralları ekle.

Handler'larda yeni alanların entity'ye map edildiğini doğrula.

### Step 12.4 — Görsel yükleme altyapısı

Frontend'de görsel seçildiğinde backend'e URL değil, base64 veya multipart form data gelecek. Bu aşamada **URL kabul eden basit yaklaşım** yeterli — frontend şimdilik harici URL girebiliyor.

İleride görsel yükleme eklenecekse `IFileStorageService` interface'i Application katmanına ekle (şimdilik stub):

```csharp
// Application/Common/Interfaces/IFileStorageService.cs
public interface IFileStorageService
{
    Task<string> UploadImageAsync(Stream imageStream, string fileName, CancellationToken ct);
}
```

Şimdilik infrastructure implementasyonu: konsola log at, URL'i olduğu gibi kaydet. Gerçek blob storage Phase 12.5'te gelecek.

### Step 12.5 — Görsel yükleme endpoint'i (opsiyonel, ilerisi için)

`Api/Controllers/ItemsController.cs`'e ekle:
```
POST /api/items/{itemId}/image
```
- `[Consumes("multipart/form-data")]`
- Max 5MB, sadece `image/jpeg` ve `image/png`
- `IFileStorageService.UploadImageAsync` çağır
- Dönen URL'i `Item.ImageUrl`'e kaydet
- Şimdilik `IFileStorageService` implementasyonu konsola log atar ve statik URL döner

`INotificationService`'e `ItemImageUpdatedAsync` ekle.

**Checkpoint**: `POST /api/lists/{listId}/items` body'sine `imageUrl` ve `planningNote` eklenebiliyor. `GET /api/lists/{listId}/items` yanıtında bu alanlar dönüyor. `PUT /api/items/{itemId}` ile güncellenebiliyor.

---

## Phase 13 — Dashboard için List Summary DTO Genişletme

### Step 13.1 — GetUserListsQuery DTO güncelleme

Mevcut `GetUserListsQuery` muhtemelen sadece liste adı ve ID döndürüyor. Frontend dashboard (Image 2) şunlara ihtiyaç duyuyor:

```csharp
public class ListSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MemberRole CurrentUserRole { get; set; }
    public int MemberCount { get; set; }
    public int TotalItemCount { get; set; }
    public int PurchasedItemCount { get; set; }
    public int PendingItemCount { get; set; }
    public decimal CompletionPercentage { get; set; }  // PurchasedItemCount / TotalItemCount * 100
    public List<MemberAvatarDto> Members { get; set; } = new();  // avatar stack için
    public DateTime CreatedAt { get; set; }
}

public class MemberAvatarDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;  // DisplayName'den üret
}
```

`GetUserListsQuery` handler'ını güncelle: tüm verileri tek `.Select()` projeksiyonunda hesapla — ayrı ayrı sorgu açma.

```csharp
// Handler içinde örnek projeksiyon:
.Select(list => new ListSummaryDto
{
    Id = list.Id,
    Name = list.Name,
    CurrentUserRole = list.Members
        .First(m => m.UserId == currentUserId).Role,
    MemberCount = list.Members.Count(),
    TotalItemCount = list.Items.Count(),
    PurchasedItemCount = list.Items.Count(i => i.Status == ItemStatus.Purchased),
    PendingItemCount = list.Items.Count(i => i.Status == ItemStatus.Pending),
    CompletionPercentage = list.Items.Any()
        ? Math.Round((decimal)list.Items.Count(i => i.Status == ItemStatus.Purchased)
            / list.Items.Count() * 100, 1)
        : 0,
    Members = list.Members
        .Take(3)  // avatar stack için max 3, +N göstergesi frontend'de hesaplanır
        .Select(m => new MemberAvatarDto
        {
            UserId = m.UserId,
            DisplayName = m.User.DisplayName,
            Initials = m.User.DisplayName.Length >= 2
                ? m.User.DisplayName.Substring(0, 2).ToUpper()
                : m.User.DisplayName.ToUpper()
        }).ToList(),
    CreatedAt = list.CreatedAt
})
```

### Step 13.2 — GetListByIdQuery DTO güncelleme

Frontend proje dashboard'u (Image 3) şunları tek API çağrısında istiyor:
- Finansal özet (toplam hedef, harcanan)
- Bekleyen talepler (pending claims) — sadece dashboard'da gösterim için kısa özet
- Kategori bazlı ilerleme kartları

Mevcut `GetListByIdQuery` response DTO'sunu genişlet:

```csharp
public class ListDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MemberRole CurrentUserRole { get; set; }
    public decimal CompletionPercentage { get; set; }

    // Finansal özet
    public FinancialSummaryDto Financial { get; set; } = new();

    // Bekleyen talepler (dashboard widget için)
    public List<PendingClaimSummaryDto> PendingClaims { get; set; } = new();

    // Kategori bazlı özet kartları
    public List<CategorySummaryDto> CategorySummaries { get; set; } = new();

    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FinancialSummaryDto
{
    public decimal TotalEstimated { get; set; }   // tüm seçili option fiyatlarının toplamı
    public decimal TotalSpent { get; set; }        // satın alınan item'ların seçili option fiyatı
    public decimal RemainingBudget { get; set; }   // TotalEstimated - TotalSpent
}

public class PendingClaimSummaryDto
{
    public Guid ClaimId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string OptionTitle { get; set; } = string.Empty;
    public string ClaimantDisplayName { get; set; } = string.Empty;
    public int Percentage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CategorySummaryDto
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomLabel { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int PurchasedItems { get; set; }
    public decimal CompletionPercentage { get; set; }
    public string Description { get; set; } = string.Empty;  // frontend'de gösterilecek açıklama
    public List<MemberAvatarDto> AssignedMembers { get; set; } = new();  // o kategoride item'ı olan üyeler
}
```

Handler güncelleme: tüm hesaplamalar tek sorguda `.Select()` ile yapılmalı. `PendingClaims` için sadece `Status=Pending` olan claim'leri, `OptionClaim` → `ItemOption` → `Item` join'iyle getir. Max 5 pending claim döndür (dashboard widget için yeterli).

`AssignedMembers` hesabı: kategori altındaki item'ları purchase eden ya da claim'i olan üyelerin distinct listesi, max 3 avatar döner.

**Checkpoint**: `GET /api/lists/{listId}` tek çağrıda finansal özet, bekleyen talepler ve kategori kartları döndürüyor. `GET /api/lists` dashboard için gereken tüm liste istatistiklerini döndürüyor.

---

## Phase 14 — Davet Yönetimi Endpoint'leri

### Step 14.1 — GetPendingInvitesQuery

`Application/Features/Members/GetPendingInvitesQuery.cs` — `ListId`:
- Sadece Owner çağırabilir
- `AcceptedAt=null` ve `ExpiresAt > UtcNow` olan davetleri döndür
- Süresi geçmiş davetleri de ayrı filtreyle döndür (frontend'de "süresi dolmuş" göstergesi için)

```csharp
public class PendingInviteDto
{
    public Guid InviteId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public MemberRole Role { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsExpired { get; set; }  // ExpiresAt < UtcNow
}
```

### Step 14.2 — CancelInviteCommand

`Application/Features/Members/CancelInviteCommand.cs` — `InviteId`:
- Sadece Owner çağırabilir
- Davet bu listeye ait olmalı, değilse `NotFoundException`
- Davet zaten kabul edilmişse `DomainException`: "Kabul edilmiş davet iptal edilemez"
- Kaydı hard delete et — soft delete yok

### Step 14.3 — ResendInviteCommand

`Application/Features/Members/ResendInviteCommand.cs` — `InviteId`:
- Sadece Owner çağırabilir
- Mevcut daveti bulur, `ExpiresAt`'ı `UtcNow + 7 gün` olarak günceller
- `IEmailService.SendInviteAsync` tekrar çağır (stub)

### Step 14.4 — Controller güncellemesi

`Api/Controllers/MembersController.cs`'e ekle:
- `GET    /api/lists/{listId}/members/invites` — GetPendingInvitesQuery
- `DELETE /api/lists/{listId}/members/invites/{inviteId}` — CancelInviteCommand
- `POST   /api/lists/{listId}/members/invites/{inviteId}/resend` — ResendInviteCommand

**Checkpoint**: Owner bekleyen davetleri görebiliyor, iptal edebiliyor, yeniden gönderebiliyor. Image 8'deki "Bekleyen Davetler" listesi ve `✕` butonu bu endpoint'lerle çalışıyor.

---

## Phase 15 — Raporlar Endpoint Genişletme

Frontend raporlar sayfası (Image 7) mevcut endpoint'lerden fazlasını istiyor.

### Step 15.1 — GetListSummaryQuery güncelleme

Mevcut `GetListSummaryQuery` dönen DTO'ya ekle:

```csharp
public class ListSummaryReportDto
{
    // mevcut alanlar...
    public decimal BudgetUsagePercentage { get; set; }  // TotalSpent / TotalEstimated * 100
    public decimal RemainingBudget { get; set; }

    // yeni — raporlar sayfasındaki "Hazırlık Durumu" kartı için
    public decimal ReadinessPercentage { get; set; }    // purchased / total * 100
    public string ReadinessLabel { get; set; } = string.Empty;
    // "Tamamlandı" / "İyi Gidiyor" / "Devam Ediyor" / "Yeni Başladı"
    // 80%+ → Tamamlandı, 50-79% → İyi Gidiyor, 20-49% → Devam Ediyor, 0-19% → Yeni Başladı
}
```

### Step 15.2 — GetItemsForReportQuery

Raporlar sayfasındaki "İtem Listesi & Durum" tablosu için düz liste. Mevcut `GetItemsByListQuery`'den farklı — bu sıralı, tüm kategorileri tek listede ve görsel URL'i de içeriyor.

`Application/Features/Reports/GetItemsForReportQuery.cs`:
- Tüm list üyeleri okuyabilir
- Dönen: item başına `imageUrl`, `name`, `selectedOptionTitle` (seçili option varsa), `categoryName`, `estimatedPrice` (seçili option fiyatı), `status`
- Sıralama: önce Purchased, sonra Pending; her grup içinde category'ye göre alfabetik

```csharp
public class ReportItemDto
{
    public Guid Id { get; set; }
    public string? ImageUrl { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SelectedOptionTitle { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal? EstimatedPrice { get; set; }
    public string? Currency { get; set; }
    public ItemStatus Status { get; set; }
}
```

Controller: `Api/Controllers/ReportsController.cs`'e ekle:
- `GET /api/lists/{listId}/reports/items` — GetItemsForReportQuery

### Step 15.3 — GetCategoryReportQuery güncelleme

Mevcut `GetCategoryBreportQuery` DTO'suna ekle:

```csharp
public class CategoryReportDto
{
    // mevcut alanlar...
    public decimal CompletionPercentage { get; set; }  // SegmentedProgressBar için
    // "X/Y İTEM ALINDI" gösterimi frontend'de hesaplanır ama backend doğru veri döndürmeli
}
```

**Checkpoint**: `GET /api/lists/{listId}/reports/summary` `budgetUsagePercentage` ve `readinessPercentage` döndürüyor. `GET /api/lists/{listId}/reports/items` endpoint'i çalışıyor, görsel URL dahil.

---

## Phase 16 — Item Detail Sayfası için Ek Endpoint'ler

Mevcut `GET /api/items/{itemId}/options` item detay sayfası (Image 5) için yeterliydi ama tam sayfa olunca bazı ek veriler gerekiyor.

### Step 16.1 — GetItemDetailQuery

`Application/Features/Items/GetItemDetailQuery.cs` — `ItemId`:

Mevcut `GetItemsByListQuery` item listesini döndürüyor; tek item detayı için ayrı query ekle:

```csharp
public class ItemDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? PlanningNote { get; set; }
    public ItemStatus Status { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Kategori bilgisi
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string RoomLabel { get; set; } = string.Empty;

    // Atanan üye (şimdilik Owner — ilerisi için)
    public MemberAvatarDto AssignedTo { get; set; } = new();

    // Seçenekler (mevcut GetOptionsByItemQuery ile aynı veri ama tek çağrıda)
    public List<ItemOptionDetailDto> Options { get; set; } = new();
}
```

`ItemOptionDetailDto` mevcut `ItemOptionDto`'yu kapsar + rating ve claim özetlerini içerir (Phase 9 ve 11'de eklenenleri dahil et).

Controller: `Api/Controllers/ItemsController.cs`'e ekle:
- `GET /api/items/{itemId}` — GetItemDetailQuery

### Step 16.2 — UpdatedAt alanı

`Item` entity'sine `UpdatedAt` alanı ekle:

```csharp
public DateTime UpdatedAt { get; set; }
```

`ItemConfiguration.cs`'de: nullable değil, default `DateTime.UtcNow`.

Her `UpdateItemCommand`, `MarkItemPurchasedCommand` handler'ında `item.UpdatedAt = _dateTimeService.UtcNow` set et.

Migration:
```
dotnet ef migrations add AddUpdatedAtToItem --project Infrastructure --startup-project Api
```

"Son Güncelleme: 2 saat önce" gösterimi için frontend `UpdatedAt`'ı kullanır; backend sadece UTC datetime döner.

**Checkpoint**: `GET /api/items/{itemId}` tek çağrıda item detayını, seçeneklerini, rating'lerini ve claim'lerini döndürüyor. Image 5'teki sağ panel verileri (durum, son güncelleme, atanan) bu endpoint'ten geliyor.

---

## Phase 17 — Bildirim Sistemi Genişletme

Image 3'te "Bekleyen Talepler" widget'ı ve "3 Yeni" badge görünüyor. Bu badge için frontend'in anlık bildirim sayısına ihtiyacı var.

### Step 17.1 — GetNotificationCountQuery

`Application/Features/Notifications/GetNotificationCountQuery.cs` — `ListId`:
- Sadece Owner okuyabilir
- Döner:
```csharp
public class NotificationCountDto
{
    public int PendingClaimsCount { get; set; }
    public int PendingInvitesCount { get; set; }
    public int TotalNew { get; set; }  // PendingClaimsCount + PendingInvitesCount
}
```

Controller: `Api/Controllers/ListsController.cs`'e ekle:
- `GET /api/lists/{listId}/notifications/count` — GetNotificationCountQuery

### Step 17.2 — SignalR bildirim sayısı güncellemesi

`INotificationService`'e ekle:
```csharp
Task NotificationCountChangedAsync(Guid listId, Guid ownerUserId, NotificationCountDto count, CancellationToken ct);
```

`ClaimCreatedAsync` sonrasında ve `ReviewClaimCommand` sonrasında `NotificationCountChangedAsync` çağır — böylece Owner'ın ekranındaki "3 Yeni" badge'i anlık güncellenir.

`SignalRNotificationService`'de `NotificationCountChangedAsync` implementasyonu: claim oluşturulunca tüm gruba değil sadece Owner'a gönder. Hub'da `Clients.User(ownerUserId.ToString()).SendAsync(...)` kullan — tüm grup değil.

**Checkpoint**: Owner listeye girdiğinde bildirim sayısını çekebiliyor. Yeni claim geldiğinde Owner'ın badge'i SignalR ile anlık güncelleniyor.

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
