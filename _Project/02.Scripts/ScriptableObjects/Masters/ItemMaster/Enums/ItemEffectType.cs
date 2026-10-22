/// <summary>
/// 아이템 특수효과의 기능 타입.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>(0) BonusAttack - 추가 공격 효과</description></item>
/// <item><description>(1) Skill - 공격 또는 버프 등의 스킬 효과</description></item>
/// <item><description>(2) Buff - 파티 강화 효과</description></item>
/// <item><description>(3) Debuff - 적 약화 효과</description></item>
/// <item><description>(4) Heal - 회복 효과</description></item>
/// </list>
/// </remarks>
public enum ItemEffectType
{
    /// <summary>(0) BonusAttack - 추가 공격 효과</summary>
    BonusAttack,

    /// <summary>(1) Skill - 공격 또는 버프 등의 스킬 효과</summary>
    Skill,

    /// <summary>(2) Buff - 파티 강화 효과</summary>
    Buff,

    /// <summary>(3) Debuff - 적 약화 효과</summary>
    Debuff,

    /// <summary>(4) Heal - 회복 효과</summary>
    Heal,

    /// <summary>Length 대신 사용하기 위한 Count 변수</summary>
    Count
}
