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
- `IEmailService.SendInviteAsync` tekrar çağır (bkz. Phase 18)

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

## Phase 18 — Resend Email Entegrasyonu (Invite Mail)

Bu phase mevcut `IEmailService` stub'ını gerçek Resend implementasyonuyla değiştirir.
Davet maili akışı: Owner "Davet Et"e tıklar → `InviteMemberCommand` handler çalışır → `IEmailService.SendInviteAsync` çağrılır → Resend API üzerinden mail gider.

### Step 18.1 — NuGet paketi

```
dotnet add Infrastructure package Resend
```

`Infrastructure.csproj`'a eklenen paket: `Resend` (resmi .NET SDK).

### Step 18.2 — Konfigürasyon

`appsettings.json`'a ekle:

```json
"Resend": {
  "ApiKey": "",
  "FromEmail": "davet@yourdomain.com",
  "FromName": "2gather"
}
```

`appsettings.Development.json`'da (git-ignored) gerçek API key'i tut:

```json
"Resend": {
  "ApiKey": "re_xxxxxxxxxxxxxxxxxxxx",
  "FromEmail": "onboarding@resend.dev",
  "FromName": "2gather"
}
```

> **Not**: Development'ta kendi domain'ini doğrulamadan önce Resend'in `onboarding@resend.dev` adresini kullanabilirsin. Bu adres sadece Resend hesabına kayıtlı e-posta adresine gönderim yapar.

`Infrastructure/Settings/ResendOptions.cs` oluştur:

```csharp
public class ResendOptions
{
    public const string SectionName = "Resend";
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
```

### Step 18.3 — IEmailService interface güncelleme

`Application/Common/Interfaces/IEmailService.cs`'i kontrol et. Eğer stub hâlâ boşsa veya yoksa şu şekilde tanımla:

```csharp
public interface IEmailService
{
    Task SendInviteAsync(
        string toEmail,
        string listName,
        string inviterName,
        string inviteToken,
        MemberRole role,
        CancellationToken ct);
}
```

Bu interface Application katmanında kalır — Infrastructure'a referans vermez.

### Step 18.4 — ResendEmailService implementasyonu

`Infrastructure/Services/ResendEmailService.cs` oluştur:

```csharp
using Microsoft.Extensions.Options;
using Resend;
using Application.Common.Interfaces;
using Domain.Enums;
using Infrastructure.Options;

public sealed class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailService> _logger;

    public ResendEmailService(
        IResend resend,
        IOptions<ResendOptions> options,
        ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendInviteAsync(
        string toEmail,
        string listName,
        string inviterName,
        string inviteToken,
        MemberRole role,
        CancellationToken ct)
    {
        var inviteUrl = $"{_options.AppBaseUrl}/invite/accept?token={inviteToken}";
        var roleLabel = role switch
        {
            MemberRole.Editor => "Editör",
            MemberRole.Viewer => "İzleyici",
            _ => role.ToString()
        };

        var message = new EmailMessage
        {
            From = $"{_options.FromName} <{_options.FromEmail}>",
            To = { toEmail },
            Subject = $"{inviterName} sizi \"{listName}\" listesine davet etti",
            HtmlBody = BuildInviteHtml(inviterName, listName, roleLabel, inviteUrl)
        };

        try
        {
            await _resend.EmailSendAsync(message, ct);
            _logger.LogInformation(
                "Invite email sent to {Email} for list {ListName}", toEmail, listName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send invite email to {Email} for list {ListName}", toEmail, listName);
            throw;
        }
    }

    private static string BuildInviteHtml(
        string inviterName,
        string listName,
        string roleLabel,
        string inviteUrl) => $"""
        <!DOCTYPE html>
        <html lang="tr">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>2gather — Davet</title>
        </head>
        <body style="margin:0;padding:0;background-color:#F5F3EE;font-family:'Inter',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#F5F3EE;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="560" cellpadding="0" cellspacing="0"
                       style="background:#FFFFFF;border-radius:16px;overflow:hidden;border:1px solid #E8E6E0;">

                  <!-- Header -->
                  <tr>
                    <td style="background:#3D5A4C;padding:32px 40px;">
                      <p style="margin:0;font-size:22px;font-weight:700;color:#FFFFFF;
                                letter-spacing:-0.5px;">2gather</p>
                      <p style="margin:4px 0 0;font-size:12px;color:#B8CBB8;
                                letter-spacing:0.05em;">BİRLİKTE PLANLAYIN</p>
                    </td>
                  </tr>

                  <!-- Body -->
                  <tr>
                    <td style="padding:40px;">
                      <p style="margin:0 0 8px;font-size:11px;color:#9B9B9B;
                                letter-spacing:0.1em;text-transform:uppercase;">Davet</p>
                      <h1 style="margin:0 0 24px;font-size:28px;font-weight:700;
                                 color:#1A1A1A;line-height:1.2;">
                        Sizi bir listeye davet ettiler
                      </h1>

                      <p style="margin:0 0 24px;font-size:15px;color:#6B6B6B;line-height:1.6;">
                        <strong style="color:#1A1A1A;">{inviterName}</strong>,
                        sizi <strong style="color:#1A1A1A;">"{listName}"</strong>
                        listesine <strong style="color:#3D5A4C;">{roleLabel}</strong>
                        olarak davet etti.
                      </p>

                      <!-- Role Info Box -->
                      <table width="100%" cellpadding="0" cellspacing="0"
                             style="background:#EEF2EF;border-radius:10px;margin-bottom:32px;">
                        <tr>
                          <td style="padding:16px 20px;">
                            <p style="margin:0;font-size:13px;color:#3D5A4C;font-weight:600;">
                              {roleLabel} olarak şunları yapabilirsiniz:
                            </p>
                            <p style="margin:6px 0 0;font-size:13px;color:#6B8F7A;line-height:1.5;">
                              {GetRoleDescription(roleLabel)}
                            </p>
                          </td>
                        </tr>
                      </table>

                      <!-- CTA Button -->
                      <table cellpadding="0" cellspacing="0">
                        <tr>
                          <td style="background:#3D5A4C;border-radius:10px;">
                            <a href="{inviteUrl}"
                               style="display:inline-block;padding:14px 32px;font-size:15px;
                                      font-weight:600;color:#FFFFFF;text-decoration:none;">
                              Daveti Kabul Et →
                            </a>
                          </td>
                        </tr>
                      </table>

                      <p style="margin:24px 0 0;font-size:13px;color:#9B9B9B;line-height:1.5;">
                        Bu davet 7 gün içinde geçerliliğini yitirir. Butona tıklayamazsanız
                        aşağıdaki bağlantıyı tarayıcınıza kopyalayın:
                      </p>
                      <p style="margin:8px 0 0;font-size:12px;color:#9B9B9B;word-break:break-all;">
                        {inviteUrl}
                      </p>
                    </td>
                  </tr>

                  <!-- Footer -->
                  <tr>
                    <td style="padding:24px 40px;border-top:1px solid #E8E6E0;">
                      <p style="margin:0;font-size:12px;color:#9B9B9B;">
                        Bu daveti siz istemediyseniz bu e-postayı görmezden gelebilirsiniz.
                        Hesabınız güvende.
                      </p>
                      <p style="margin:8px 0 0;font-size:12px;color:#9B9B9B;">
                        © 2gather — Birlikte Planlayın
                      </p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    private static string GetRoleDescription(string roleLabel) => roleLabel switch
    {
        "Editör" => "Liste üzerinde item ve seçenek ekleyebilir, düzenleyebilir ve işaretleyebilirsiniz.",
        "İzleyici" => "Listeyi görüntüleyebilir, ilerlemeyi takip edebilirsiniz.",
        _ => "Liste üzerinde işlem yapabilirsiniz."
    };
}
```

### Step 18.5 — AppBaseUrl konfigürasyonu

`ResendOptions`'a `AppBaseUrl` ekle (davet linki için):

```csharp
public class ResendOptions
{
    public const string SectionName = "Resend";
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string AppBaseUrl { get; set; } = string.Empty;
}
```

`appsettings.json`'a ekle:

```json
"Resend": {
  "ApiKey": "",
  "FromEmail": "davet@yourdomain.com",
  "FromName": "2gather",
  "AppBaseUrl": "https://yourdomain.com"
}
```

`appsettings.Development.json`'da:

```json
"Resend": {
  "ApiKey": "re_xxxxxxxxxxxxxxxxxxxx",
  "FromEmail": "onboarding@resend.dev",
  "FromName": "2gather",
  "AppBaseUrl": "http://localhost:5173"
}
```

### Step 18.6 — DI kaydı

`Infrastructure/DependencyInjection.cs` (veya `Program.cs`'deki `AddInfrastructureServices` extension'ı) içinde:

```csharp
// Resend options
services.Configure<ResendOptions>(
    configuration.GetSection(ResendOptions.SectionName));

// Resend SDK — AddResend() extension method mevcut değil, manual kayıt gerekiyor
services.Configure<ResendClientOptions>(o =>
    o.ApiToken = configuration["Resend:ApiKey"] ?? string.Empty);
services.AddHttpClient<ResendClient>();
services.AddTransient<IResend, ResendClient>();

// IEmailService implementasyonu — mevcut stub varsa kaldır, bunu ekle
services.AddScoped<IEmailService, ResendEmailService>();
```

### Step 18.7 — InviteMemberCommand handler güncelleme

`Application/Features/Members/InviteMemberCommand.cs` handler'ında `IEmailService.SendInviteAsync` çağrısının zaten var olduğunu doğrula. Eğer stub sırasında yorum satırı veya `// TODO` olarak bırakıldıysa aktif hale getir:

```csharp
// Handler içinde — invite kaydedildikten sonra:
await _emailService.SendInviteAsync(
    toEmail: command.Email,
    listName: list.Name,
    inviterName: currentUser.DisplayName,
    inviteToken: invite.Token,
    role: command.Role,
    ct: cancellationToken);
```

`ResendInviteCommand` handler'ında da aynı çağrıyı yap — `ExpiresAt` güncellendikten sonra.

### Step 18.8 — CLAUDE.md güncellemesi

`CLAUDE.md §14 Configuration`'daki `Email:SmtpHost` satırını kaldır, yerine ekle:

```
Resend:ApiKey, Resend:FromEmail, Resend:FromName, Resend:AppBaseUrl
```

**Checkpoint**: 
- `POST /api/lists/{listId}/members/invite` çağrısı sonrasında davet e-postası Resend üzerinden gönderiliyor
- `POST /api/lists/{listId}/members/invites/{inviteId}/resend` çağrısında mail yeniden gönderiliyor
- Mail şablonu 2gather tasarım diline uygun (koyu yeşil header, davet butonu, rol açıklaması)
- API key environment variable'da, kaynak kodda yok
- `dotnet build` sıfır hata

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
