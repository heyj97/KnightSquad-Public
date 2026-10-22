using System.Collections.Generic;

// 인벤토리 슬롯 수와 장비 판매가격.
public static partial class GlobalConstants
{
    // 인벤토리 최대 갯수
    /// <summary>
    /// 사용자 인벤토리에 보관할 수 있는 최대 슬롯 수입니다.
    /// </summary>
    public const int InventoryMaxSlotCount = 200;

    // Max를 포함한 인스턴스별 판매가격 범위. 획득한 장비의 가격은 이후 유지한다.
    public static readonly Dictionary<SystemRarity, (int Min, int Max)> EquipmentSalePriceTable = new()
    {
        { SystemRarity.Common, (3, 7) },
        { SystemRarity.Uncommon, (10, 20) },
        { SystemRarity.Rare, (30, 60) },
        { SystemRarity.Epic, (80, 150) },
        { SystemRarity.Legendary, (200, 350) },
        { SystemRarity.Mystic, (500, 800) }
    };

    public static bool TryGetEquipmentSalePriceRange(SystemRarity rarity, out int min, out int max)
    {
        min = max = 0;
        if (!EquipmentSalePriceTable.TryGetValue(rarity, out var range) ||
            range.Min <= 0 || range.Max < range.Min || range.Max == int.MaxValue) return false;
        min = range.Min;
        max = range.Max;
        return true;
    }

    public static int CreateEquipmentSalePrice(SystemRarity rarity)
        => TryGetEquipmentSalePriceRange(rarity, out int min, out int max)
            ? UnityEngine.Random.Range(min, max + 1) : 0;
}
