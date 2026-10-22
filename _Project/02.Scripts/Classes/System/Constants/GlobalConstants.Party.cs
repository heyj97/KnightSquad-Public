using System.Collections.Generic;

// 파티 성장 효과와 강화 비용.
public static partial class GlobalConstants
{
    // 레벨당 스텟비율
    /// <summary>
    /// 파티 최대 체력 강화 레벨당 추가되는 비율입니다.
    /// </summary>
    public const float HpMultiplierPerLv = 0.2f;
    /// <summary>
    /// 파티 공격력 강화 레벨당 추가되는 비율입니다.
    /// </summary>
    public const float AttackMultiplierPerLv = 0.2f;
    /// <summary>
    /// 파티 공격속도 강화 레벨당 추가되는 비율입니다.
    /// </summary>
    public const float AttackSpeedMultiplierPerLv = 0.2f;
    /// <summary>
    /// 파티 방어력 강화 레벨당 추가되는 비율입니다.
    /// </summary>
    public const float DefenseMultiplierPerLv = 0.2f;
    /// <summary>
    /// 파티 치명타 확률 강화 레벨당 더해지는 확률값입니다.
    /// </summary>
    public const float CriticalChancePerLv = 0.02f;
    /// <summary>
    /// 파티 치명타 피해 강화 레벨당 더해지는 배율입니다.
    /// </summary>
    public const float CriticalDamageMultiplierPerLv = 0.12f;

    /// <summary>
    /// 각 파티 성장 능력치가 도달할 수 있는 최대 레벨입니다.
    /// </summary>
    public const int MaxPartyGrowthLevel = 10;

    /// <summary>
    /// 현재 성장 레벨을 인덱스로 사용하여 다음 강화에 필요한 골드를 제공합니다.
    /// </summary>
    public static readonly List<int> PartyGoldSpendTable = new()
    {
        10, // 1 > 2레벨
        30, // 2 > 3레벨
        50, // 3 > 4레벨
        80, // 4 > 5레벨
        120, // 5 > 6레벨
        150, // 6 > 7레벨
        180, // 7 > 8레벨
        210, // 8 > 9레벨
        300, // 9 > 10레벨
    };
}
