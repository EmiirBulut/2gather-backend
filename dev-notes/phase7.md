# Phase 7 — Bug Fixes

Completed: 2026-04-15
Branch: phase7/step-7-1

---

## Step 7.1 — Silme ve güncelleme düzeltmeleri

**Sorun:** `DeleteOptionCommandHandler` ve `UpdateOptionCommandHandler` iki ayrı sorgu kullanıyordu:
1. `IOptionRepository.GetByIdAsync` ile option fetch
2. `IItemRepository.GetByIdAsync` ile item fetch (ListId'ye ulaşmak için)

**Düzeltme:**
- `IOptionRepository`'ye `GetByIdWithItemAsync(Guid id)` eklendi
- `OptionRepository`'de `.Include(o => o.Item)` ile tek sorguda option + item navigation getirildi
- `DeleteOptionCommandHandler` ve `UpdateOptionCommandHandler` `GetByIdWithItemAsync` kullanacak şekilde güncellendi
- Her iki handler'dan `IItemRepository` bağımlılığı kaldırıldı

`DeleteItemCommandHandler` ve `UpdateItemCommandHandler` zaten doğruydu — item fetch + member check yapıyorlardı.

---

## Step 7.2 — Ondalıklı tutar precision

Değişiklik gerekmedi:
- `ItemOptionConfiguration.cs`: `HasPrecision(18, 2)` zaten tanımlıydı ✅
- `ItemOptionDto`: `Price` alanı zaten `decimal?` tipindeydi ✅

---

## Step 7.3 — Options count düzeltmesi

**Sorun:** `ItemRepository.GetByListIdAsync` `.Include(i => i.Options)` kullanıyordu — tüm option verisini yalnızca sayısını almak için çekiyordu.

**Düzeltme:**
- `IItemRepository.GetByListIdAsync` dönüş tipi `IReadOnlyList<Item>` → `IReadOnlyList<(Item item, int optionsCount)>` olarak değiştirildi
- `ItemRepository.GetByListIdAsync` içinde `Include` kaldırıldı, EF Core subquery ile count hesaplandı:
  ```csharp
  OptionsCount = _dbContext.ItemOptions.Count(o => o.ItemId == i.Id)
  ```
  Oluşturulan SQL: `SELECT ..., (SELECT COUNT(*) FROM "ItemOptions" WHERE "ItemId" = i."Id") AS count FROM "Items"`
- `GetItemsByListQueryHandler` tuple kullanacak şekilde güncellendi

---

## Step 7.4 — Satın alındı butonu

Değişiklik gerekmedi — tüm adımlar zaten doğruydu:
- `Status = ItemStatus.Purchased` ✅
- `PurchasedAt = IDateTimeService.UtcNow` ✅
- `SaveChangesAsync` çağrılıyor ✅
- `INotificationService.ItemPurchasedAsync` çağrılıyor ✅
- Controller: `PATCH /api/items/{itemId}/status` doğru tanımlı ✅

---

## Test Sonuçları

**21 test, 0 hata** — mevcut testler etkilenmedi.
