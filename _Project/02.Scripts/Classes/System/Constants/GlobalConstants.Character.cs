using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// 캐릭터 최대 레벨·기본 능력치 성장·중복 소비 요구량.
public static partial class GlobalConstants
{
    public const float CharacterMaxLevelBaseMultiplier = 2f;

    /// <summary>
    /// 캐릭터 등급에 따라 허용되는 최대 레벨을 반환합니다.
    /// </summary>
    /// <param name="rarity">조회할 캐릭터 또는 아이템 등급입니다.</param>
    /// <returns>지정 등급의 캐릭터가 도달할 수 있는 최대 레벨입니다.</returns>
    /// <exception cref="ArgumentOutOfRangeException">인수가 지원되는 범위를 벗어난 경우 발생합니다.</exception>
    public static int GetCharacterMaxLevel(SystemRarity rarity)
    {
        return rarity switch
        {
            SystemRarity.Common => 60,
            SystemRarity.Uncommon => 50,
            SystemRarity.Rare => 40,
            SystemRarity.Epic => 30,
            SystemRarity.Legendary => 20,
            SystemRarity.Mystic => 10,
            _ => throw new ArgumentOutOfRangeException(
                nameof(rarity), rarity, "유효하지 않은 캐릭터 등급입니다.")
        };
    }

    /// <summary>
    /// 인덱스는 도달할 레벨, 값은 해당 레벨업에 필요한 동일 캐릭터 중복 수.
    /// 최초 획득은 Lv.1이며 인덱스 0과 1은 0이다. 만렙 초과 항목은 없다.
    /// </summary>
    public static readonly IReadOnlyDictionary<SystemRarity, IReadOnlyList<int>> CharacterLevelDuplicateRequirementTable =
        new ReadOnlyDictionary<SystemRarity, IReadOnlyList<int>>(
            new Dictionary<SystemRarity, IReadOnlyList<int>>
            {
                // Lv.2~20 / Lv.21~40 / Lv.41~최대 레벨 순서
                { SystemRarity.Common, CreateCharacterDuplicateRequirements(SystemRarity.Common, 1, 2, 3) },
                { SystemRarity.Uncommon, CreateCharacterDuplicateRequirements(SystemRarity.Uncommon, 1, 2, 3) },
                { SystemRarity.Rare, CreateCharacterDuplicateRequirements(SystemRarity.Rare, 1, 2) },
                { SystemRarity.Epic, CreateCharacterDuplicateRequirements(SystemRarity.Epic, 1, 2) },
                { SystemRarity.Legendary, CreateCharacterDuplicateRequirements(SystemRarity.Legendary, 1) },
                { SystemRarity.Mystic, CreateCharacterDuplicateRequirements(SystemRarity.Mystic, 1) }
            });

    /// <summary>
    /// 캐릭터가 목표 레벨에 도달할 때 소비해야 하는 중복 보유 수를 반환합니다.
    /// </summary>
    /// <param name="rarity">조회할 캐릭터 또는 아이템 등급입니다.</param>
    /// <param name="targetLevel">도달하려는 캐릭터 레벨입니다.</param>
    /// <returns>지정 목표 레벨에 도달할 때 소비할 동일 캐릭터 중복 수입니다.</returns>
    /// <exception cref="ArgumentOutOfRangeException">인수가 지원되는 범위를 벗어난 경우 발생합니다.</exception>
    public static int GetCharacterLevelDuplicateRequirement(SystemRarity rarity, int targetLevel)
    {
        if (!CharacterLevelDuplicateRequirementTable.TryGetValue(rarity, out IReadOnlyList<int> requirements))
            throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "유효하지 않은 캐릭터 등급입니다.");
        if (targetLevel < 2 || targetLevel >= requirements.Count)
            throw new ArgumentOutOfRangeException(nameof(targetLevel), targetLevel, "레벨업 목표는 Lv.2부터 최대 레벨까지입니다.");
        return requirements[targetLevel];
    }

    private static IReadOnlyList<int> CreateCharacterDuplicateRequirements(
        SystemRarity rarity, params int[] requirementsBy20Levels)
    {
        int maxLevel = GetCharacterMaxLevel(rarity);
        int[] requirements = new int[maxLevel + 1];
        for (int level = 2; level <= maxLevel; level++)
            requirements[level] = requirementsBy20Levels[(level - 1) / 20];
        return Array.AsReadOnly(requirements);
    }
}
