using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// 뽑기 비용·확률·천장·만렙 중복 전환량.
public static partial class GlobalConstants
{
    public const ulong CharacterGachaGoldPrice = 1000;
    public const ulong CharacterGachaRMPrice = 100;
    public const int CharacterGachaDailyGoldLimit = 10;
    public const int CharacterGachaResetUtcOffsetHours = 9; // 한국 시간 오전 0시

    // SO를 MasterDataMgr에 등록한 뒤 ID만 추가하면 천장 선택 목록에 표시된다.
    public static readonly IReadOnlyList<string> CharacterGachaPityTargetIds = Array.AsReadOnly(new[]
        { "CHAR_MYSTIC_TANKER_DEUKGWANG" });

    public static readonly IReadOnlyDictionary<SystemRarity, int> CharacterStarFragmentTable =
        new ReadOnlyDictionary<SystemRarity, int>(new Dictionary<SystemRarity, int>
        {
            [SystemRarity.Common] = 1, [SystemRarity.Uncommon] = 2, [SystemRarity.Rare] = 5,
            [SystemRarity.Epic] = 10, [SystemRarity.Legendary] = 25, [SystemRarity.Mystic] = 100
        });

    /// <summary>
    /// 선택 천장 보상을 적용하는 캐릭터 뽑기 누적 횟수입니다.
    /// </summary>
    public const int CharacterGachaPityCount = 100;

    /// <summary>일반 추첨의 등급별 확률(0~1). 선택 천장은 별도 처리한다.</summary>
    public static readonly IReadOnlyDictionary<SystemRarity, float> CharacterGachaRarityRateTable =
        new ReadOnlyDictionary<SystemRarity, float>(new Dictionary<SystemRarity, float>
        {
            { SystemRarity.Common, 0.36f },
            { SystemRarity.Uncommon, 0.28f },
            { SystemRarity.Rare, 0.18f },
            { SystemRarity.Epic, 0.12f },
            { SystemRarity.Legendary, 0.05f },
            { SystemRarity.Mystic, 0.01f }
        });

    /// <summary>
    /// 일반 캐릭터 뽑기에서 지정 등급이 등장할 확률을 반환합니다.
    /// </summary>
    /// <param name="rarity">조회할 캐릭터 또는 아이템 등급입니다.</param>
    /// <returns>0부터 1 사이로 표현한 지정 등급의 등장 확률입니다.</returns>
    /// <exception cref="ArgumentOutOfRangeException">인수가 지원되는 범위를 벗어난 경우 발생합니다.</exception>
    public static float GetCharacterGachaRarityRate(SystemRarity rarity)
    {
        if (!CharacterGachaRarityRateTable.TryGetValue(rarity, out float rate))
            throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "유효하지 않은 캐릭터 등급입니다.");
        return rate;
    }
}
