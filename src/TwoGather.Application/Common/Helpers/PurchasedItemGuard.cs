using TwoGather.Domain.Entities;
using TwoGather.Domain.Enums;
using TwoGather.Domain.Exceptions;

namespace TwoGather.Application.Common.Helpers;

/// <summary>A purchased item is locked: its options, ratings, claims and details can no longer change.</summary>
public static class PurchasedItemGuard
{
    public const string LockedMessage = "Satın alınmış ürün üzerinde değişiklik yapılamaz.";

    public static void EnsureNotPurchased(Item item)
    {
        if (item.Status == ItemStatus.Purchased)
            throw new DomainException(LockedMessage);
    }
}
