/// <summary>
/// 시스템에서 사용하는 희귀도.(캐릭터, 아이템)
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>(0) Common - 하급</description></item>
/// <item><description>(1) Uncommon - 중급</description></item>
/// <item><description>(2) Rare - 상급</description></item>
/// <item><description>(3) Epic - 최상급</description></item>
/// <item><description>(4) Legendary - 전설급</description></item>
/// <item><description>(5) Mystic - 신화급</description></item>
/// <item><description>Count: 6</description></item>
/// </list>
/// </remarks>
public enum SystemRarity
{
    /// <summary>(0) Common - 하급</summary>
    Common,

    /// <summary>(1) Uncommon - 중급</summary>
    Uncommon,

    /// <summary>(2) Rare - 상급</summary>
    Rare,

    /// <summary>(3) Epic - 최상급</summary>
    Epic,

    /// <summary>(4) Legendary - 전설급</summary>
    Legendary,

    /// <summary>(5) Mystic - 신화급</summary>
    Mystic,

    /// <summary>Length 대신 사용하기 위한 Count 변수</summary>
    Count
}